namespace JoinRpg.Web.Schedule;

public class ProgramItemViewModel
{
    /// <summary>
    /// Пустая ячейка сетки: мероприятия в этом слоте и помещении нет, поэтому и
    /// <see cref="Id"/> нет. Раньше здесь стоял -1, но типизированные id
    /// неположительных значений не принимают.
    /// </summary>
    public static ProgramItemViewModel Empty { get; } = new ProgramItemViewModel()
    {
        Id = null,
        Name = "",
        Description = null,
        Users = [],
        IsEmpty = true,
    };
    public required CharacterIdentification? Id { get; set; }
    public required string Name { get; set; }
    public required MarkdownString? Description { get; set; }
    public required UserLinkViewModel[] Users { get; set; }

    public bool IsEmpty { get; private set; } = false;

    public int RowSpan { get; set; } = 1;
    public int ColSpan { get; set; } = 1;
}
