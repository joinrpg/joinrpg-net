namespace JoinRpg.DomainTypes.Plots;

public enum PlotElementType
{
    RegularPlot,
    Handout,

    /// <summary>
    /// Вводная-сетка ролей: вместо текста у персонажа отображается сохранённая сетка ролей
    /// (ADR029).
    /// </summary>
    RoleGrid,
}
