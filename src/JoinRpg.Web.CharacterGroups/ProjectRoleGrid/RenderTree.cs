namespace JoinRpg.Web.CharacterGroups.ProjectRoleGrid;

/// <summary>
/// Дерево узлов для клиентского рендера режима дерева. В отличие от плоского
/// <see cref="ProjectRoleGridViewModel.Rows"/> (DFS-развёртка с явной Depth), тут у каждой
/// группы есть прямой список детей — это и есть «соседи», которых можно переставлять
/// кнопками JoinMoveControl без похода на сервер за всей сеткой заново.
/// </summary>
public abstract record RenderNode;

public sealed record GroupNode(ProjectRoleGridGroupHeaderRowViewModel Header, List<RenderNode> Children) : RenderNode;

public sealed record CharBlockNode(List<ProjectRoleGridCharacterRowViewModel> Characters) : RenderNode;

public static class RenderTreeBuilder
{
    /// <summary>
    /// Восстанавливает дерево из плоского DFS-списка строк по их явной Depth.
    /// Собственный блок персонажей группы всегда идёт первым среди её детей (перед
    /// дочерними группами), как и в исходном плоском порядке.
    /// </summary>
    public static GroupNode? Build(IReadOnlyList<ProjectRoleGridRowViewModel> rows)
    {
        var stack = new List<(int Depth, List<RenderNode> Children)>();
        GroupNode? root = null;
        CharBlockNode? currentCharBlock = null;

        foreach (var row in rows)
        {
            if (row is ProjectRoleGridGroupHeaderRowViewModel groupHeader)
            {
                while (stack.Count > 0 && stack[^1].Depth >= groupHeader.Depth)
                {
                    stack.RemoveAt(stack.Count - 1);
                }

                var node = new GroupNode(groupHeader, []);
                if (stack.Count == 0)
                {
                    root = node;
                }
                else
                {
                    stack[^1].Children.Add(node);
                }

                stack.Add((groupHeader.Depth, node.Children));
                currentCharBlock = null;
            }
            else if (row is ProjectRoleGridCharacterRowViewModel charRow && stack.Count > 0)
            {
                if (currentCharBlock is null)
                {
                    currentCharBlock = new CharBlockNode([]);
                    stack[^1].Children.Add(currentCharBlock);
                }
                currentCharBlock.Characters.Add(charRow);
            }
        }

        return root;
    }
}
