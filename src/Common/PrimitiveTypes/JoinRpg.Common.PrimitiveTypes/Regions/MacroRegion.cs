namespace JoinRpg.Common.PrimitiveTypes;

/// <summary>
/// Макрорегион КогдаИгры (kogda-igra.ru) — раздел сайта, объединяющий субрегионы <see cref="Region"/>.
/// Числовые значения равны region_id из справочника КогдаИгры и не должны меняться.
/// Справочник снят с db/init/02-reference-data.sql репозитория joinrpg/kogda-igra (снят 2026-11-25);
/// расхождение с живой КогдаИгрой чинится отдельной задачей, а не молчаливой правкой значений.
/// Псевдо-регион «Все игры» (1) не включён. Казахстан (12) в справочнике регионов отсутствует,
/// но на него ссылаются области Казахстана — добавлен по данным ki_sub_regions.
/// </summary>
public enum MacroRegion
{
    NorthWest = 2,
    MoscowAndCenter = 3,
    Ural = 5,
    Siberia = 6,
    South = 7,
    FarEast = 8,
    Volga = 9,
    Ukraine = 10,
    Belarus = 11,
    Kazakhstan = 12,
    Foreign = 13,
}
