namespace JoinRpg.Services.Interfaces.Projects;

public enum ProjectTypeDto
{
    Larp,
    Convention,
    ConventionProgram,
    CopyFromAnother,
    EmptyProject
}

public enum ProjectCopySettingsDto
{
    SettingsAndFields,
    SettingsFieldsGroupsAndCharacters,
    SettingsFieldsGroupsCharactersAndPlot,
}
