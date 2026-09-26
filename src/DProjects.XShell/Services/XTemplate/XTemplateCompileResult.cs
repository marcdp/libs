namespace DProjects.XShell.Services.XTemplate {

    public sealed record XTemplateDependency(string Resource, IReadOnlyList<IReadOnlyList<string>> AncestorPaths);

    public sealed record XTemplateCompileResult(string RenderJavaScript, IReadOnlyList<XTemplateDependency> Dependencies, IReadOnlyList<string> Slots);
}
