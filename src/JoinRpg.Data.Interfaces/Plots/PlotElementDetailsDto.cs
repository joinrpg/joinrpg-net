using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Plots;

namespace JoinRpg.Data.Interfaces.Plots;

/// <summary>
/// Вводная в том виде, в котором её показывает страница сюжета: одна конкретная версия
/// плюс метаданные соседних, чтобы нарисовать переходы «предыдущая/следующая».
/// </summary>
/// <remarks>
/// Тексты остальных версий сюда намеренно не входят: страницам нужен контент только
/// отображаемой версии, а у крупных папок история правок — основной объём данных.
/// </remarks>
/// <param name="Id">Идентификатор вводной.</param>
/// <param name="ElementType">Тип вводной (обычная, входящая в группу и т.п.).</param>
/// <param name="IsMasterOnly">Вводная видна только мастерам.</param>
/// <param name="IsActive">Вводная не удалена.</param>
/// <param name="PublishedVersion">Номер опубликованной версии, <c>null</c> если не опубликована ни одна.</param>
/// <param name="Target">Персонажи и группы, которым адресована вводная.</param>
/// <param name="PlotFolderMasterTitle">Название папки сюжета — страницы показывают его в заголовке.</param>
/// <param name="CurrentVersion">Отображаемая версия.</param>
/// <param name="LastVersionNumber">
/// Номер последней версии вводной. Нужен отдельно от <paramref name="CurrentVersion"/>:
/// статус вводной считается по последней версии, а показывать можно любую.
/// </param>
/// <param name="PrevVersionModifiedAt">Дата правки предыдущей версии, <c>null</c> если отображается первая.</param>
/// <param name="NextVersionModifiedAt">Дата правки следующей версии, <c>null</c> если отображается последняя.</param>
public record PlotElementDetailsDto(
    PlotElementIdentification Id,
    PlotElementType ElementType,
    bool IsMasterOnly,
    bool IsActive,
    int? PublishedVersion,
    TargetsInfo Target,
    string PlotFolderMasterTitle,
    PlotElementVersionDto CurrentVersion,
    int LastVersionNumber,
    DateTime? PrevVersionModifiedAt,
    DateTime? NextVersionModifiedAt)
{
    /// <summary>Отображаемая версия — та же, что опубликована.</summary>
    public bool IsCurrentVersionPublished => PublishedVersion == CurrentVersion.Version;

    /// <summary>Опубликована последняя версия — по вводной нечего доделывать.</summary>
    public bool IsLastVersionPublished => PublishedVersion == LastVersionNumber;
}

/// <summary>
/// Одна версия текста вводной.
/// </summary>
/// <param name="Version">Номер версии.</param>
/// <param name="Content">Текст вводной (markdown).</param>
/// <param name="TodoField">Мастерское TODO по этой версии.</param>
/// <param name="ModifiedAt">Когда версия была сохранена.</param>
/// <param name="Author">Кто сохранил версию, <c>null</c> для старых записей без автора.</param>
public record PlotElementVersionDto(
    int Version,
    MarkdownDbValue Content,
    string TodoField,
    DateTime ModifiedAt,
    UserInfoHeader? Author);
