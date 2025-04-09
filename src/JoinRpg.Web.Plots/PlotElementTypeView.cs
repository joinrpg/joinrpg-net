using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Plots;

public enum PlotElementTypeView
{
    [Display(Name = "Обычная вводная", Description = "Текст, который нужно выдать игроку.")]
    RegularPlot,
    [Display(Name = "Элемент раздатки", Description = "Инструкция службе регистрации выдать какой-то определенный предмет игроку. Одна строка — один предмет.")]
    Handout,
}
