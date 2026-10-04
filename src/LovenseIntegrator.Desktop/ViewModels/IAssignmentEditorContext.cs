using LovenseIntegrator.Core;

namespace LovenseIntegrator.Desktop.ViewModels;

public interface IAssignmentEditorContext
{
    RuleEditor Draft { get; }
    string Summary { get; }
    IReadOnlyList<Choice<string>> Applications { get; }
    IReadOnlyList<Toy> Targets { get; }
    IReadOnlyList<Rule> EffectSources { get; }
    void RefreshApplications();
}
