namespace JoinRpg.Common.WebComponents.Badges;

/// <summary>Строка бейджа: уже вычисленный текст и его раскладка</summary>
/// <param name="Text">Текст строки</param>
/// <param name="OffsetMm">Отступ строки от края бейджа, мм: верхнего края от верха или, при <paramref name="FromBottom"/>, нижнего от низа</param>
/// <param name="MinFontPt">Минимальный размер шрифта, пт</param>
/// <param name="MaxFontPt">Максимальный размер шрифта, пт</param>
/// <param name="Bold">Полужирный шрифт</param>
/// <param name="FromBottom">Отступ отсчитывается от низа бейджа; перенесённая на вторую строку растёт вверх</param>
public record PrintableBadgeLine(string Text, int OffsetMm, int MinFontPt, int MaxFontPt, bool Bold, bool FromBottom = false);
