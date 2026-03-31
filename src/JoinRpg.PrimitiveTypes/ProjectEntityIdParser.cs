using System.Diagnostics.CodeAnalysis;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.PrimitiveTypes.Forums;
using JoinRpg.PrimitiveTypes.Plots;
using JoinRpg.PrimitiveTypes.ProjectMetadata.Payments;

namespace JoinRpg.PrimitiveTypes;

public static class ProjectEntityIdParser
{
    public static bool TryParseId(ReadOnlySpan<char> value, [MaybeNullWhen(false)] out IProjectEntityId id)
    {
        value = value.Trim();

        if (!value.Contains('('))
        {
            id = null;
            return false;
        }
        if (ProjectIdentification.TryParse(value, null, out var project))
        {
            id = project;
            return true;
        }

        if (PaymentTypeIdentification.TryParse(value, null, out var pt))
        {
            id = pt;
            return true;
        }

        if (ClaimCommentIdentification.TryParse(value, null, out var cci))
        {
            id = cci;
            return true;
        }

        if (ForumCommentIdentification.TryParse(value, null, out var fci))
        {
            id = fci;
            return true;
        }

        if (ForumThreadIdentification.TryParse(value, null, out var fti))
        {
            id = fti;
            return true;
        }

        if (FinanceOperationIdentification.TryParse(value, null, out var fo))
        {
            id = fo;
            return true;
        }

        if (PlotVersionIdentification.TryParse(value, null, out var pv))
        {
            id = pv;
            return true;
        }

        if (PlotElementIdentification.TryParse(value, null, out var pe))
        {
            id = pe;
            return true;
        }

        if (PlotFolderIdentification.TryParse(value, null, out var pf))
        {
            id = pf;
            return true;
        }

        if (ClaimIdentification.TryParse(value, null, out var claim))
        {
            id = claim;
            return true;
        }

        if (CharacterIdentification.TryParse(value, null, out var character))
        {
            id = character;
            return true;
        }

        if (CharacterGroupIdentification.TryParse(value, null, out var charGroup))
        {
            id = charGroup;
            return true;
        }

        if (ProjectFieldVariantIdentification.TryParse(value, null, out var pfv))
        {
            id = pfv;
            return true;
        }

        if (ProjectFieldIdentification.TryParse(value, null, out var pfi))
        {
            id = pfi;
            return true;
        }

        id = null;
        return false;
    }
}
