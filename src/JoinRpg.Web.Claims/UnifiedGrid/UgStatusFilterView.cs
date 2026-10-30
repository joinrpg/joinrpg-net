using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Claims.UnifiedGrid;

public enum UgStatusFilterView
{
    /// <summary>
    /// Активные персонажи + активные заявки на них
    /// </summary>
    [Display(Name = "Активные", Description = "Активные персонажи и активные заявки")]
    Active,
    /// <summary>
    /// Активные персонажи без утвержденных заявок + активные заявки на них
    /// </summary>
    [Display(Name = "Вакансии", Description = "Не занятые персонажи")]
    Vacant,
    /// <summary>
    /// Активные непринятые заявки + персонажи к ним
    /// </summary>
    [Display(Name = "Обсуждаются", Description = "Персонажи с обсуждаемыми заявками")]
    Discussion,
    /// <summary>
    /// Активные персонажи с принятой заявкой; в строке — только принятая заявка
    /// </summary>
    [Display(Name = "Приняты", Description = "Принятые заявки")]
    Approved,
    /// <summary>
    /// Активные персонажи с заявками в листе ожидания; в строке — только эти заявки
    /// </summary>
    [Display(Name = "Лист ожидания", Description = "Заявки в листе ожидания")]
    OnHold,
    /// <summary>
    /// (Удаленные персонажи + все заявки на них) + (активные персонажи с отклоненными и отозванными заявками + отклоненные/отозванные заявки)
    /// </summary>
    [Display(Name = "Архив", Description = "Удаленные персонажи, отклоненные и отозванные заявки")]
    Archive,
    /// <summary>
    /// Все персонажи, включая удаленных, и все их заявки
    /// </summary>
    [Display(Name = "Все", Description = "Все персонажи и все их заявки")]
    All,
}
