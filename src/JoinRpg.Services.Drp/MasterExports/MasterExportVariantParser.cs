using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.Services.Drp.MasterExports;

/// <summary>
/// Разбирает текст «подпись, подпись, …» обратно в варианты поля.
/// </summary>
/// <remarks>
/// <para>
/// Выгрузка склеивает подписи через «, » в порядке <see cref="ProjectFieldInfo.Variants"/>, а подпись сама
/// может содержать «, ». Поэтому текст режется по разделителю на куски, и динамикой по кускам
/// ищется минимальное число нераспознанных кусков. Затем перебираются все раскладки с этим минимумом
/// (не больше <see cref="MaxAlternatives"/>); одинаково подписанные варианты дают отдельные раскладки.
/// </para>
/// <para>
/// Если нераспознанных кусков нет, раскладки, чьё отображение (подписи в порядке
/// <see cref="ProjectFieldInfo.Variants"/> через «, ») не совпадает с исходным текстом, отбрасываются —
/// выгрузка такую дать не могла. Если совпадений нет совсем — остаются все раскладки.
/// Неоднозначность — когда осталось больше одного различного набора вариантов. Из оставшихся
/// выбирается раскладка с меньшим числом сегментов (длинные подписи важнее), затем с меньшим числом
/// выключенных вариантов, затем первая по порядку вариантов.
/// </para>
/// </remarks>
internal static class MasterExportVariantParser
{
    private const string Separator = ", ";

    /// <summary>Сколько раскладок перебирать — дальше неоднозначность очевидна и так.</summary>
    private const int MaxAlternatives = 64;

    public record Result(IReadOnlyList<ProjectFieldVariant> Variants, bool HasUnknown, bool IsAmbiguous);

    /// <summary>Сегмент раскладки: до куска <c>Next</c> (не включая); <c>Label</c> null — кусок не распознан.</summary>
    private record struct Step(int Next, string? Label);

    /// <summary>Неизменяемый список сегментов раскладки — общий префикс у альтернатив не копируется.</summary>
    private sealed record Path(ProjectFieldVariant? Variant, Path? Previous);

    public static Result Parse(ProjectFieldInfo field, string text)
    {
        var byLabel = field.Variants
            .GroupBy(v => v.Label.Trim(), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
        var maxLabelLength = byLabel.Keys.Select(l => l.Length).DefaultIfEmpty(0).Max();

        var tokens = text.Split(Separator);
        var n = tokens.Length;

        // unknowns[k] — минимум нераспознанных кусков в раскладке суффикса с куска k;
        // steps[k] — первые сегменты, с которыми этот минимум достигается.
        var unknowns = new int[n + 1];
        var steps = new List<Step>[n + 1];
        steps[n] = [];

        for (var k = n - 1; k >= 0; k--)
        {
            var options = new List<(Step Step, int Unknowns)> { (new Step(k + 1, null), unknowns[k + 1] + 1) };

            // Кандидат строится инкрементально и обрывается, когда стал длиннее любой подписи
            var candidate = tokens[k];
            for (var m = k + 1; m <= n; m++)
            {
                if (m > k + 1)
                {
                    candidate = candidate + Separator + tokens[m - 1];
                }
                var trimmed = candidate.Trim();
                if (trimmed.Length > maxLabelLength)
                {
                    break;
                }
                if (byLabel.ContainsKey(trimmed))
                {
                    options.Add((new Step(m, trimmed), unknowns[m]));
                }
            }

            unknowns[k] = options.Min(o => o.Unknowns);
            steps[k] = [.. options.Where(o => o.Unknowns == unknowns[k]).Select(o => o.Step)];
        }

        var alternatives = EnumerateAlternatives(steps, byLabel, n);
        var hasUnknown = unknowns[0] > 0;

        var candidates = alternatives;
        if (!hasUnknown)
        {
            var expected = text.Trim();
            var matching = alternatives.Where(a => Display(field, a.Variants) == expected).ToList();
            if (matching.Count > 0)
            {
                candidates = matching;
            }
        }

        var distinctSets = candidates
            .Select(a => string.Join(",", field.Variants.Where(a.Variants.Contains).Select(v => v.Id.ProjectFieldVariantId)))
            .Distinct()
            .Count();

        var best = candidates
            .OrderBy(a => a.Segments)
            .ThenBy(a => a.Variants.Count(v => !v.IsActive))
            .First();

        return new Result([.. field.Variants.Where(best.Variants.Contains)], hasUnknown, distinctSets > 1);
    }

    private static string Display(ProjectFieldInfo field, HashSet<ProjectFieldVariant> variants)
        => string.Join(Separator, field.Variants.Where(variants.Contains).Select(v => v.Label)).Trim();

    private static List<(HashSet<ProjectFieldVariant> Variants, int Segments)> EnumerateAlternatives(
        List<Step>[] steps,
        Dictionary<string, List<ProjectFieldVariant>> byLabel,
        int n)
    {
        var result = new List<(HashSet<ProjectFieldVariant>, int)>();
        // Обход в глубину без рекурсии: кусков может быть много.
        // Тупиков нет — каждый шаг в steps ведёт к раскладке с минимумом нераспознанных.
        var stack = new Stack<(int Position, Path? Path, int Segments)>();
        stack.Push((0, null, 0));

        while (stack.Count > 0 && result.Count < MaxAlternatives)
        {
            var (position, path, segments) = stack.Pop();
            if (position == n)
            {
                var variants = new HashSet<ProjectFieldVariant>();
                for (var p = path; p is not null; p = p.Previous)
                {
                    if (p.Variant is not null)
                    {
                        _ = variants.Add(p.Variant);
                    }
                }
                result.Add((variants, segments));
                continue;
            }

            // В обратном порядке, чтобы первыми снимались первые шаги и варианты
            for (var i = steps[position].Count - 1; i >= 0; i--)
            {
                var step = steps[position][i];
                if (step.Label is null)
                {
                    stack.Push((step.Next, new Path(null, path), segments + 1));
                    continue;
                }
                var sameLabel = byLabel[step.Label];
                for (var j = sameLabel.Count - 1; j >= 0; j--)
                {
                    stack.Push((step.Next, new Path(sameLabel[j], path), segments + 1));
                }
            }
        }

        return result;
    }
}
