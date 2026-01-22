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
    /// (Удаленные персонажи + все заявки на них) + (активные персонажи с отклоненными и отозванными заявками + отклоненные/отозванные заявки)
    /// </summary>
    [Display(Name = "Архив", Description = "Удаленные персонажи, отклоненные и отозванные заявки")]
    Archive,
}
