using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace BeamNG.JBeam;

public partial class JBeamFile
{
    public static IEnumerable<Part> GetPartsList(IEnumerable<JBeamFile> files)
    {
        foreach (var file in files)
            foreach (var part in file.Parts)
                yield return part;
    }

    public partial class PartsTree  // AST
    {
        public enum NodeType{  Part, Slot  }
        public class Node
        {
            public NodeType Type { get; }
            public Node? Parent { get; private set; }
            public List<Node> Children { get; } = new();

            public Part? Part { get; }  // only valid when Type == Part
            public Slot? Slot { get; }  // only valid when Type == Slot

            [MemberNotNullWhen( true, nameof(Part))]
            public bool IsPart => Type == NodeType.Part;
            [MemberNotNullWhen(true, nameof(Slot))]
            public bool IsSlot => Type == NodeType.Slot;

            public string? PartIdentifier => Part?.Identifier;
            public List<string> PartSlotType => Part?.SlotType?.Items ?? new List<string>();
            public string? SlotName => Slot?.Name;

            public Node(Part part)
            {
                Type = NodeType.Part; 
                Part = part; 
            }
            public Node(Slot slot)
            {
                Type = NodeType.Slot;
                Slot = slot;
            }

            public Node Add(Node child)
            {
                child.Parent = this;
                Children.Add(child);
                return child;
            }
        }

    }

    public partial class PartsTree  // Building
    {
        public List<Node> Roots { get; } = new();

        public PartsTree(IEnumerable<Node> roots)
            => Roots.AddRange(roots);
        public PartsTree(IEnumerable<Part> parts)
            => Roots = new Builder(parts).Build();
        public PartsTree(IEnumerable<JBeamFile> files)
            : this(GetPartsList(files)) { } 
    }

    public partial class PartsTree
    {
        private class Builder
        {
            private readonly IEnumerable<Part> _parts;
            private readonly Dictionary<string, Node> _partNodes = new();
            private readonly HashSet<string> _expanded = new();

            public Builder(IEnumerable<Part> parts)
            {
                _parts = parts;
            }

            public List<Node> Build()
            {
                // Create unique Part nodes
                foreach (var part in _parts)
                    _partNodes[part.Identifier] = new Node(part);

                // Expand each part exactly once
                foreach (var part in _parts)
                    Expand(_partNodes[part.Identifier], new HashSet<string> { part.Identifier });

                // Roots = nodes with no parent
                return _partNodes.Values.Where(n => n.Parent == null).ToList();
            }

            private void Expand(Node parent, HashSet<string> visited)
            {
                // Prevent duplicate expansion
                if (parent.IsPart)
                {
                    var id = parent.Part!.Identifier;
                    if (_expanded.Contains(id))
                        return;

                    _expanded.Add(id);
                }

                var slots = parent.Part?.Slots?.Items;
                if (slots == null)
                    return;

                foreach (var slot in slots)
                {
                    var allowed = _parts
                        .Where(p => p.SlotType != null && slot.IsAllowed(p.SlotType.Items))
                        .Where(p => !visited.Contains(p.Identifier))
                        .ToList();

                    if (allowed.Count == 0)
                        continue;

                    var slotNode = parent.Add(new Node(slot));

                    foreach (var childPart in allowed)
                    {
                        var childNode = _partNodes[childPart.Identifier];
                        slotNode.Add(childNode);

                        visited.Add(childPart.Identifier);
                        Expand(childNode, visited);
                        visited.Remove(childPart.Identifier);
                    }
                }
            }
        }
    }

    public partial class PartsTree  // Printing
    {
        public override string ToString() => TreePrinter.Print(Roots);

        public static class TreePrinter
        {
            private const string Vertical = "\u2502";      // │
            private const string Branch   = "\u251C";      // ├
            private const string Corner   = "\u2514";      // └
            private const string Line     = "\u2500";      // ─

            public static string Print(IEnumerable<Node> roots)
            {
                var sb = new StringBuilder();

                foreach (var root in roots)
                    PrintNode(root, "", true, new HashSet<string>(), sb);

                return sb.ToString();
            }

            private static void PrintNode(Node node, string prefix, bool isLast, HashSet<string> visitedParts, StringBuilder sb)
            {
                // Cycle detection
                if (node.IsPart)
                {
                    if (visitedParts.Contains(node.Part.Identifier))
                    {
                        sb.Append(prefix)
                        .Append(Corner).Append(Line).Append(Line)
                        .Append(" [cycle detected: ").Append(NodeLabel(node)).Append("]\n");
                        return;
                    }

                    visitedParts.Add(node.Part.Identifier);
                }

                // Choose branch characters
                string branch = isLast
                    ? Corner + " "     // └─
                    : Branch + " ";   // ├   

                // Print this node
                sb.Append(prefix).Append(branch).Append(NodeLabel(node)).Append('\n');

                // Prepare next prefix
                string childPrefix = prefix + (isLast ? "  " : Vertical + " ");

                // Print children
                for (int i = 0; i < node.Children.Count; i++)
                {
                    bool lastChild = (i == node.Children.Count - 1);
                    PrintNode(node.Children[i], childPrefix, lastChild, visitedParts, sb);
                }

                if (node.IsPart)
                    visitedParts.Remove(node.Part.Identifier);
            }

            private static string NodeLabel(Node node)
            {
                return node.Type switch
                {
                    NodeType.Part => "Part: " + node.Part!.Identifier,
                    NodeType.Slot => SlotLabel(node.Slot!),
                    _ => "Unknown"
                };
            }

            private static string SlotLabel(Slot slot)
            {
                var allow = string.Join(", ", slot.AllowTypes);
                var deny  = string.Join(", ", slot.DenyTypes);

                return "Slot: " + slot.Name;
                    //+ " (allow: [" + allow + "] deny: [" + deny + "])";
            }
        }

    }
    
}