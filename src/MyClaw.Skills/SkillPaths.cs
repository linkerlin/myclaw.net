namespace MyClaw.Skills;

public static class SkillPaths
{
    public static string ResolveSkillsDirectory(string workspace, string? configuredDir = null)
    {
        return string.IsNullOrWhiteSpace(configuredDir)
            ? Path.Combine(workspace, "skills")
            : configuredDir;
    }

    public static string ResolveSkillDirectory(string workspace, string name, string? configuredDir = null)
    {
        return Path.Combine(ResolveSkillsDirectory(workspace, configuredDir), name);
    }

    public static string ResolveSkillFilePath(string workspace, string name, string? configuredDir = null)
    {
        return Path.Combine(ResolveSkillDirectory(workspace, name, configuredDir), "SKILL.md");
    }

    public static string ResolveLegacySkillFilePath(string workspace, string name, string? configuredDir = null)
    {
        return Path.Combine(ResolveSkillsDirectory(workspace, configuredDir), $"{name}.md");
    }
}