namespace JoinRpg.Common.WebComponents.Badges;

/// <summary>Строка бейджа: уже вычисленный текст и его раскладка</summary>
/// <param name="Text">Текст строки</param>
/// <param name="TopMm">Отступ верхнего края строки от верха бейджа, мм</param>
/// <param name="MinFontPt">Минимальный размер шрифта, пт</param>
/// <param name="MaxFontPt">Максимальный размер шрифта, пт</param>
/// <param name="Bold">Полужирный шрифт</param>
public record PrintableBadgeLine(string Text, int TopMm, int MinFontPt, int MaxFontPt, bool Bold);
