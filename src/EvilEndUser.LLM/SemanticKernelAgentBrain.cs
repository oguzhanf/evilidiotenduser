using Azure.AI.OpenAI;
using Azure.Identity;
using EvilEndUser.Core.Abstractions;
using EvilEndUser.Core.Models;
using EvilEndUser.LLM.Plugins;
using EvilEndUser.LLM.Prompts;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace EvilEndUser.LLM;

/// <summary>
/// The LLM-backed agent brain. It builds a Semantic Kernel over the configured provider (Azure
/// OpenAI / OpenAI / Ollama), exposes the simulator's actions to the model as auto-invocable tools,
/// and drives the simulated user's non-deterministic behaviour. All tool calls flow through the
/// scenario context, so the safety guard and telemetry always apply.
/// </summary>
public sealed class SemanticKernelAgentBrain : IAgentBrain
{
    private readonly LlmConfig _llm;
    private readonly SimulationConfig _config;
    private readonly ILogger<SemanticKernelAgentBrain>? _logger;

    /// <summary>Creates the brain from configuration.</summary>
    public SemanticKernelAgentBrain(SimulationConfig config, ILogger<SemanticKernelAgentBrain>? logger = null)
    {
        _config = config;
        _llm = config.Llm;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsEnabled => _llm.Provider != LlmProvider.None && _llm.IsConfigured;

    /// <inheritdoc />
    public async Task<SimAction> DecideNextActionAsync(
        IScenarioContext context, string situation, CancellationToken ct = default)
    {
        // The wander loop is the primary entry point; for a single decision we run one step and
        // synthesise an Idle action describing what was done (the tool already executed it).
        await RunStepsAsync(context, situation, maxSteps: 1, ct);
        return new SimAction { Type = SimActionType.Idle, Rationale = "LLM single-step decision executed." };
    }

    /// <inheritdoc />
    public Task WanderAsync(IScenarioContext context, int maxSteps, CancellationToken ct = default)
    {
        var opening = "You just sat down at your PC. Your inbox has a few unread messages and your " +
                      "browser is open. Start doing your 'work'.";
        return RunStepsAsync(context, opening, maxSteps, ct);
    }

    private async Task RunStepsAsync(IScenarioContext context, string situation, int maxSteps, CancellationToken ct)
    {
        if (!IsEnabled)
        {
            context.Log("LLM brain is not configured; skipping.", EventSeverity.Warning, "AgentBrain");
            return;
        }

        var kernel = BuildKernel();
        var plugin = new UserActionPlugin(context);
        plugin.SetCancellation(ct);
        kernel.Plugins.AddFromObject(plugin, "UserActions");

        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory(PersonaPrompts.BuildSystemPrompt(_config));
        history.AddUserMessage(PersonaPrompts.BuildSituation(situation));

        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            // Blend the configured base temperature with the persona's erraticness for variety.
            Temperature = Math.Clamp(_llm.BaseTemperature * 0.5 + _config.Persona.Erraticness * 0.7, 0.0, 2.0),
            MaxTokens = _llm.MaxTokens,
        };

        for (var step = 0; step < maxSteps; step++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var response = await chat.GetChatMessageContentAsync(history, settings, kernel, ct);
                history.Add(response);

                if (!string.IsNullOrWhiteSpace(response.Content))
                {
                    context.Log($"🧠 {response.Content}", EventSeverity.Info, "AgentBrain");
                }

                history.AddUserMessage("What do you do next? Pick one action and use a tool.");
                await context.ThinkAsync(ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "LLM step failed");
                context.Log($"LLM step failed: {ex.Message}", EventSeverity.Error, "AgentBrain");
                break;
            }
        }
    }

    /// <summary>Builds a fresh kernel wired to the configured chat-completion provider.</summary>
    private Kernel BuildKernel()
    {
        var builder = Kernel.CreateBuilder();

        switch (_llm.Provider)
        {
            case LlmProvider.AzureOpenAI:
                if (!string.IsNullOrWhiteSpace(_llm.ApiKey))
                {
                    builder.AddAzureOpenAIChatCompletion(_llm.DeploymentName, _llm.Endpoint!, _llm.ApiKey);
                }
                else
                {
                    // Token-based auth (DefaultAzureCredential / device login to the demo tenant).
                    var client = new AzureOpenAIClient(new Uri(_llm.Endpoint!),
                        new DefaultAzureCredential());
                    builder.AddAzureOpenAIChatCompletion(_llm.DeploymentName, client);
                }
                break;

            case LlmProvider.OpenAI:
                builder.AddOpenAIChatCompletion(_llm.DeploymentName, _llm.ApiKey!);
                break;

            case LlmProvider.Ollama:
                // Ollama exposes an OpenAI-compatible endpoint.
                builder.AddOpenAIChatCompletion(
                    _llm.DeploymentName,
                    new Uri(_llm.Endpoint!),
                    apiKey: "ollama");
                break;

            default:
                throw new InvalidOperationException("No LLM provider configured.");
        }

        return builder.Build();
    }
}
