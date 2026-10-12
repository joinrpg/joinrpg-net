using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces;

/// <summary>
/// Репозиторий для изменения метаданных проекта. Гарантирует согласованность
/// EF-сущности <see cref="Project"/> и доменного снимка <see cref="ProjectInfo"/>.
/// </summary>
public interface IProjectMetadataWriteRepository
{
    /// <summary>
    /// Грузит трекаемый <see cref="Project"/> со всеми связями и согласованный с ним
    /// <see cref="ProjectInfo"/>.
    /// </summary>
    Task<IProjectMetadataUpdateHandle> LoadProjectForUpdate(ProjectIdentification projectId);

    /// <summary>
    /// Вставляет «оболочку» группы персонажей с заданным идентификатором — для восстановления
    /// потерянной группы из выгрузки мастера (DRP, #5529): на её прежний id ссылаются выгрузки, а EF6
    /// значение IDENTITY-колонки не вставляет. Оболочка — обычная (не спец-) активная непубличная
    /// группа с пустым описанием прямо под корневой группой проекта, как её создало бы
    /// <c>ICharacterGroupService.AddCharacterGroup</c>; порядок детей корневой группы не меняется (новая
    /// группа — вне сохранённого порядка, как и при обычном создании).
    /// </summary>
    /// <remarks>
    /// Выполняется сразу, своей транзакцией, мимо <c>SaveChanges</c> и мимо кеша метаданных проекта:
    /// вызывающий сам перечитывает метаданные. Вставляется только id ниже текущего счётчика IDENTITY —
    /// такая вставка его не двигает и параллельно создаваемым группам не мешает.
    /// </remarks>
    Task<RestoredShellInsertResult> InsertRestoredCharacterGroupShell(RestoredCharacterGroupShell shell);
}

/// <summary>
/// Минимум, без которого строки группы нет: имя, родитель (корневая группа проекта) и аудит.
/// </summary>
/// <param name="CreatedAt">Время создания и изменения (UTC) — восстановленная группа поднимает дату обновления проекта.</param>
public record RestoredCharacterGroupShell(
    CharacterGroupIdentification CharacterGroupId,
    CharacterGroupIdentification RootGroupId,
    string Name,
    UserIdentification CreatedBy,
    DateTime CreatedAt);

/// <summary>
/// Пара (трекаемый <see cref="Project"/>, согласованный с ним <see cref="ProjectInfo"/>).
/// После мутации <see cref="Project"/> вызвать <see cref="Refresh"/>, чтобы получить актуальный
/// <see cref="ProjectInfo"/> без обращения в БД.
/// </summary>
public interface IProjectMetadataUpdateHandle
{
    /// <summary>Трекаемая EF-сущность проекта; именно её нужно мутировать.</summary>
    Project Project { get; }

    /// <summary>
    /// Снимок метаданных. До вызова <see cref="Refresh"/> — состояние ДО изменения.
    /// </summary>
    ProjectInfo ProjectInfo { get; }

    /// <summary>
    /// Перечитывает <see cref="Project"/> из БД (тем же <c>DbContext</c>, значит — в той же
    /// транзакции) и пересобирает из него <see cref="ProjectInfo"/>. Перечитывание, а не пересборка
    /// по уже загруженному в памяти графу, нужно, чтобы сущности, добавленные в рамках мутации
    /// (например новый <c>ProjectAcl</c>), получили все navigation-свойства: EF6 lazy loading не
    /// работает для сущностей, созданных через <c>new</c>, а не через прокси-фабрику контекста.
    /// </summary>
    Task<ProjectInfo> Refresh();

    /// <summary>
    /// Добавляет новую под-сущность проекта в тот же <c>DbContext</c>, через который потом
    /// вызывается <c>SaveChanges</c>. Нужен там, где коллекция проекта в загрузку не входит:
    /// добавление через навигацию (<c>Project.Characters.Add</c>) лениво догрузило бы её целиком (#4965).
    /// </summary>
    void Add(object entity);

    /// <summary>
    /// Окончательно удаляет под-сущность проекта из того же <c>DbContext</c>, через который потом
    /// вызывается <c>SaveChanges</c>. Используется для permanent-delete (см. SmartDelete).
    /// </summary>
    void Remove(object entity);
}
