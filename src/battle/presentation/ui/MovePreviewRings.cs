using GrimSpace.Battle.Movement;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Ui;

public static class MovePreviewRings
{
    public readonly struct MovePreviewRingTable
    {
        public int RingCount { get; }
        private readonly int[] _shellK;
        private readonly int[][] _optionIndicesOnRing;
        public MovePreviewRingTable(int ringCount, int[] shellK, int[][] optionIndicesOnRing)
        {
            RingCount = ringCount;
            this._shellK = shellK;
            this._optionIndicesOnRing = optionIndicesOnRing;
        }
        public int ShellK(int ringIndex) => _shellK[ringIndex];
        public IReadOnlyList<int> OptionIndicesOnRing(int ringIndex) => _optionIndicesOnRing[ringIndex];
    }

    public static MovePreviewRingTable BuildRingTable(Coord position, IReadOnlyList<Option> options)
    {
        var bestApCost = new Dictionary<Coord, int>();
        var bestOption = new Dictionary<Coord, (int optionIndex, int k)>();
        for (int i = 0; i < options.Count; i++)
        {
            var option = options[i];
            var k = position.ManhattanDistanceTo(option.EndPosition);
            var apCost = option.ApCost;
            if (!bestApCost.TryGetValue(option.EndPosition, out var bestApCostForEndpoint) || apCost < bestApCostForEndpoint)
            {
                bestApCost[option.EndPosition] = apCost;
                bestOption[option.EndPosition] = (i, k);
            }
        }
        var shellKList = bestOption.Select(k => k.Value.Item2).Distinct().ToList();
        shellKList.Sort();
        var kToRingIndex = new Dictionary<int, int>();
        for (int i = 0; i < shellKList.Count; i++)
        {
            kToRingIndex.Add(shellKList[i], i);
        }
        var optionIndicesOnRingList = new List<int>[shellKList.Count];
        for (int i = 0; i < shellKList.Count; i++)
        {
            optionIndicesOnRingList[i] = new List<int>();
        }
        foreach (var (endpoint, (optionIndex, k)) in bestOption)
        {
            if (!kToRingIndex.TryGetValue(k, out var ringIndex))
            {
                throw new InvalidOperationException($"k {k} not found in kToRingIndex");
            }
            optionIndicesOnRingList[ringIndex].Add(optionIndex);
        }
        var optionIndicesOnRing = new int[shellKList.Count][];
        for (int i = 0; i < shellKList.Count; i++)
        {
            optionIndicesOnRingList[i].Sort();
            optionIndicesOnRing[i] = optionIndicesOnRingList[i].ToArray();
        }
        return new MovePreviewRingTable(shellKList.Count, shellKList.ToArray(), optionIndicesOnRing);
    }
}