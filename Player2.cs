using System;
using System.Collections.Generic;

namespace CardFool
{
    public class MPlayer2
    {
        private const string PlayerName = "Second";

        private readonly List<SCard> _hand = new List<SCard>();

        private Suits _trumpSuit;
        private int _opponentCardCount = MGameRules.TotalCards;
        private int _deckCount = MGameRules.GetDeck().Count - 2 * MGameRules.TotalCards;
        private bool _attackedThisRound;

        public string GetName()
        {
            return PlayerName;
        }

        public int GetCount()
        {
            return _hand.Count;
        }

        public void AddToHand(SCard card)
        {
            _hand.Add(card);
            SortHand();
        }

        public void SetTrump(SCard newTrump)
        {
            _trumpSuit = newTrump.Suit;
            ResetState();
        }

        public List<SCard> LayCards()
        {
            _attackedThisRound = true;
            SortHand();

            if (_hand.Count == 0)
                return new List<SCard>();

            List<int> openingGroup = SelectOpeningGroup();
            return TakeCards(openingGroup);
        }

        public bool Defend(List<SCardPair> table)
        {
            _attackedThisRound = false;

            DefensePlan plan = BuildBestDefensePlan(table);
            if (!plan.CanDefend)
                return false;

            for (int moveIndex = 0; moveIndex < plan.Moves.Count; moveIndex++)
            {
                DefenseMove move = plan.Moves[moveIndex];
                SCardPair pair = table[move.PairIndex];
                pair.SetUp(move.Card, _trumpSuit);
                table[move.PairIndex] = pair;
                RemoveCard(_hand, move.Card);
            }

            SortHand();
            return true;
        }

        public bool AddCards(List<SCardPair> table, bool opponentDefenced)
        {
            _attackedThisRound = true;

            if (table.Count >= MGameRules.TotalCards || _hand.Count == 0)
                return false;

            int defenderRemainingCards = Math.Max(0, _opponentCardCount - CountBeatenPairs(table));
            if (CountUnbeatenPairs(table) + 1 > defenderRemainingCards)
                return false;

            int cardIndex = SelectAddCardIndex(table, opponentDefenced);
            if (cardIndex < 0)
                return false;

            table.Add(new SCardPair(TakeCardAt(cardIndex)));
            return true;
        }

        public void OnEndRound(List<SCardPair> table, bool isDefenceSuccesful)
        {
            int myResolvedCount = _hand.Count;
            int opponentResolvedCount = CalculateOpponentResolvedCount(table.Count, isDefenceSuccesful);

            UpdateCountsAfterDraw(myResolvedCount, opponentResolvedCount);
            SortHand();
        }

        private void ResetState()
        {
            _hand.Clear();
            _opponentCardCount = MGameRules.TotalCards;
            _deckCount = MGameRules.GetDeck().Count - 2 * MGameRules.TotalCards;
            _attackedThisRound = false;
        }

        private void SortHand()
        {
            _hand.Sort((left, right) =>
            {
                if (left.Suit == _trumpSuit && right.Suit != _trumpSuit)
                    return 1;

                if (left.Suit != _trumpSuit && right.Suit == _trumpSuit)
                    return -1;

                int rankComparison = left.Rank.CompareTo(right.Rank);
                if (rankComparison != 0)
                    return rankComparison;

                return left.Suit.CompareTo(right.Suit);
            });
        }

        private List<int> SelectOpeningGroup()
        {
            Dictionary<int, List<int>> cardsByRank = new Dictionary<int, List<int>>();
            for (int index = 0; index < _hand.Count; index++)
            {
                int rank = _hand[index].Rank;
                if (!cardsByRank.TryGetValue(rank, out List<int>? indexes))
                {
                    indexes = new List<int>();
                    cardsByRank.Add(rank, indexes);
                }

                indexes.Add(index);
            }

            List<int> bestGroup = new List<int> { 0 };
            double bestScore = double.MinValue;
            int maxOpeningCards = Math.Max(1, Math.Min(MGameRules.TotalCards, _opponentCardCount));

            foreach (KeyValuePair<int, List<int>> pair in cardsByRank)
            {
                List<int> orderedIndexes = new List<int>(pair.Value);
                orderedIndexes.Sort((left, right) =>
                    CompareByPlayCost(_hand[left], _hand[right]));

                int limit = Math.Min(orderedIndexes.Count, maxOpeningCards);
                for (int size = 1; size <= limit; size++)
                {
                    List<int> candidate = new List<int>(size);
                    for (int offset = 0; offset < size; offset++)
                        candidate.Add(orderedIndexes[offset]);

                    double score = EvaluateOpeningGroup(candidate);
                    if (score <= bestScore)
                        continue;

                    bestScore = score;
                    bestGroup = candidate;
                }
            }

            return bestGroup;
        }

        private double EvaluateOpeningGroup(List<int> indexes)
        {
            double score = indexes.Count * 20;
            int trumpCount = 0;

            for (int index = 0; index < indexes.Count; index++)
            {
                SCard card = _hand[indexes[index]];
                if (card.Suit == _trumpSuit)
                    trumpCount++;

                score -= GetAttackCardCost(card);
            }

            if (indexes.Count > 1)
                score += 7 * (indexes.Count - 1);

            if (trumpCount == 0)
                score += 10;
            else
                score -= trumpCount * (_deckCount > 0 ? 14 : 5);

            if (_deckCount == 0)
                score += indexes.Count * 6;

            if (indexes.Count == _hand.Count)
                score += 100;

            return score;
        }

        private int SelectAddCardIndex(List<SCardPair> table, bool opponentDefenced)
        {
            int bestIndex = -1;
            double bestScore = double.MinValue;

            for (int cardIndex = 0; cardIndex < _hand.Count; cardIndex++)
            {
                SCard card = _hand[cardIndex];
                if (!CanAddCardToTable(card, table))
                    continue;

                double score = EvaluateAddCard(card, table, opponentDefenced);
                if (score <= bestScore)
                    continue;

                bestScore = score;
                bestIndex = cardIndex;
            }

            if (bestIndex < 0)
                return -1;

            double threshold = opponentDefenced ? 6 : -2;
            if (_deckCount == 0)
                threshold -= 4;

            return bestScore >= threshold ? bestIndex : -1;
        }

        private double EvaluateAddCard(SCard card, List<SCardPair> table, bool opponentDefenced)
        {
            int rankCopies = CountRankInHand(card.Rank);
            int tableMatches = CountRankOnTable(table, card.Rank);
            bool isTrump = card.Suit == _trumpSuit;

            double score = 22 - GetAttackCardCost(card);
            score += (rankCopies - 1) * 4;
            score += tableMatches * 2;

            if (_hand.Count == 1)
                score += 100;

            if (!opponentDefenced)
            {
                score += 10;

                if (_deckCount == 0)
                    score += 8;

                if (isTrump && _deckCount > 0)
                    score -= 6;
            }
            else
            {
                if (isTrump)
                    score -= _deckCount > 0 ? 16 : 6;

                if (card.Rank <= 9)
                    score += 6;

                if (_opponentCardCount <= 2)
                    score += 8;
            }

            return score;
        }

        private DefensePlan BuildBestDefensePlan(List<SCardPair> table)
        {
            List<int> unbeatenPairIndexes = new List<int>();
            for (int pairIndex = 0; pairIndex < table.Count; pairIndex++)
            {
                if (!table[pairIndex].Beaten)
                    unbeatenPairIndexes.Add(pairIndex);
            }

            if (unbeatenPairIndexes.Count == 0)
                return DefensePlan.Success(new List<DefenseMove>(), 0);

            bool[] usedCards = new bool[_hand.Count];
            bool[] assignedPairs = new bool[table.Count];
            List<DefenseMove> currentMoves = new List<DefenseMove>();
            DefensePlan bestPlan = DefensePlan.Failure();

            SearchDefense(table, unbeatenPairIndexes, usedCards, assignedPairs, currentMoves, 0, ref bestPlan);
            return bestPlan;
        }

        private void SearchDefense(
            List<SCardPair> table,
            List<int> unbeatenPairIndexes,
            bool[] usedCards,
            bool[] assignedPairs,
            List<DefenseMove> currentMoves,
            int currentCost,
            ref DefensePlan bestPlan)
        {
            if (bestPlan.CanDefend && currentCost >= bestPlan.Cost)
                return;

            if (currentMoves.Count == unbeatenPairIndexes.Count)
            {
                bestPlan = DefensePlan.Success(new List<DefenseMove>(currentMoves), currentCost);
                return;
            }

            int nextPairIndex = -1;
            List<int> candidateCardIndexes = new List<int>();
            int smallestCandidateCount = int.MaxValue;

            for (int index = 0; index < unbeatenPairIndexes.Count; index++)
            {
                int pairIndex = unbeatenPairIndexes[index];
                if (assignedPairs[pairIndex])
                    continue;

                List<int> candidates = GetDefenseCandidates(table[pairIndex].Down, usedCards);
                if (candidates.Count == 0)
                    return;

                if (candidates.Count >= smallestCandidateCount)
                    continue;

                smallestCandidateCount = candidates.Count;
                nextPairIndex = pairIndex;
                candidateCardIndexes = candidates;
            }

            if (nextPairIndex < 0)
                return;

            SCard attackCard = table[nextPairIndex].Down;
            candidateCardIndexes.Sort((left, right) =>
            {
                int leftCost = GetDefenseCardCost(attackCard, _hand[left]);
                int rightCost = GetDefenseCardCost(attackCard, _hand[right]);
                if (leftCost != rightCost)
                    return leftCost.CompareTo(rightCost);

                return CompareCards(_hand[left], _hand[right]);
            });

            assignedPairs[nextPairIndex] = true;

            for (int candidateIndex = 0; candidateIndex < candidateCardIndexes.Count; candidateIndex++)
            {
                int handIndex = candidateCardIndexes[candidateIndex];
                usedCards[handIndex] = true;

                DefenseMove move = new DefenseMove(nextPairIndex, _hand[handIndex]);
                currentMoves.Add(move);

                int nextCost = currentCost + GetDefenseCardCost(attackCard, _hand[handIndex]);
                SearchDefense(table, unbeatenPairIndexes, usedCards, assignedPairs, currentMoves, nextCost, ref bestPlan);

                currentMoves.RemoveAt(currentMoves.Count - 1);
                usedCards[handIndex] = false;
            }

            assignedPairs[nextPairIndex] = false;
        }

        private List<int> GetDefenseCandidates(SCard attackCard, bool[] usedCards)
        {
            List<int> candidates = new List<int>();
            for (int handIndex = 0; handIndex < _hand.Count; handIndex++)
            {
                if (usedCards[handIndex])
                    continue;

                if (SCard.CanBeat(attackCard, _hand[handIndex], _trumpSuit))
                    candidates.Add(handIndex);
            }

            return candidates;
        }

        private int GetDefenseCardCost(SCard attackCard, SCard defenseCard)
        {
            int cost = defenseCard.Rank * 2;
            cost += Math.Max(0, defenseCard.Rank - attackCard.Rank);

            if (defenseCard.Suit == _trumpSuit)
                cost += attackCard.Suit == _trumpSuit ? 8 : 18;
            else
                cost -= 2;

            return cost;
        }

        private List<SCard> TakeCards(List<int> indexes)
        {
            indexes.Sort();

            List<SCard> cards = new List<SCard>(indexes.Count);
            for (int index = 0; index < indexes.Count; index++)
                cards.Add(_hand[indexes[index]]);

            for (int index = indexes.Count - 1; index >= 0; index--)
                _hand.RemoveAt(indexes[index]);

            return cards;
        }

        private SCard TakeCardAt(int index)
        {
            SCard card = _hand[index];
            _hand.RemoveAt(index);
            return card;
        }

        private int CompareByPlayCost(SCard left, SCard right)
        {
            int leftCost = GetAttackCardCost(left);
            int rightCost = GetAttackCardCost(right);
            if (leftCost != rightCost)
                return leftCost.CompareTo(rightCost);

            return CompareCards(left, right);
        }

        private int CompareCards(SCard left, SCard right)
        {
            bool leftTrump = left.Suit == _trumpSuit;
            bool rightTrump = right.Suit == _trumpSuit;

            if (leftTrump != rightTrump)
                return leftTrump ? 1 : -1;

            int rankComparison = left.Rank.CompareTo(right.Rank);
            if (rankComparison != 0)
                return rankComparison;

            return left.Suit.CompareTo(right.Suit);
        }

        private int GetAttackCardCost(SCard card)
        {
            int cost = card.Rank;
            if (card.Suit == _trumpSuit)
                cost += _deckCount > 0 ? 14 : 6;

            return cost;
        }

        private int CountRankInHand(int rank)
        {
            int count = 0;
            for (int index = 0; index < _hand.Count; index++)
            {
                if (_hand[index].Rank == rank)
                    count++;
            }

            return count;
        }

        private static int CountRankOnTable(List<SCardPair> table, int rank)
        {
            int count = 0;
            for (int pairIndex = 0; pairIndex < table.Count; pairIndex++)
            {
                if (table[pairIndex].Down.Rank == rank)
                    count++;

                if (table[pairIndex].Beaten && table[pairIndex].Up.Rank == rank)
                    count++;
            }

            return count;
        }

        private static bool CanAddCardToTable(SCard card, List<SCardPair> table)
        {
            for (int pairIndex = 0; pairIndex < table.Count; pairIndex++)
            {
                if (SCardPair.CanBeAddedToPair(card, table[pairIndex]))
                    return true;
            }

            return false;
        }

        private static int CountBeatenPairs(List<SCardPair> table)
        {
            int count = 0;
            for (int pairIndex = 0; pairIndex < table.Count; pairIndex++)
            {
                if (table[pairIndex].Beaten)
                    count++;
            }

            return count;
        }

        private static int CountUnbeatenPairs(List<SCardPair> table)
        {
            return table.Count - CountBeatenPairs(table);
        }

        private static bool SameCard(SCard left, SCard right)
        {
            return left.Suit == right.Suit && left.Rank == right.Rank;
        }

        private void RemoveCard(List<SCard> cards, SCard targetCard)
        {
            for (int index = 0; index < cards.Count; index++)
            {
                if (!SameCard(cards[index], targetCard))
                    continue;

                cards.RemoveAt(index);
                return;
            }
        }

        private int CalculateOpponentResolvedCount(int attackCards, bool isDefenceSuccesful)
        {
            if (!_attackedThisRound || isDefenceSuccesful)
                return _opponentCardCount - attackCards;

            return _opponentCardCount + attackCards;
        }

        private void UpdateCountsAfterDraw(int myResolvedCount, int opponentResolvedCount)
        {
            int attackerCount = _attackedThisRound ? myResolvedCount : opponentResolvedCount;
            int defenderCount = _attackedThisRound ? opponentResolvedCount : myResolvedCount;

            attackerCount += DrawCount(attackerCount);
            defenderCount += DrawCount(defenderCount);

            _opponentCardCount = _attackedThisRound ? defenderCount : attackerCount;
        }

        private int DrawCount(int currentCount)
        {
            int draw = Math.Min(Math.Max(0, MGameRules.TotalCards - currentCount), _deckCount);
            _deckCount -= draw;
            return draw;
        }

        private readonly struct DefenseMove
        {
            public DefenseMove(int pairIndex, SCard card)
            {
                PairIndex = pairIndex;
                Card = card;
            }

            public int PairIndex { get; }
            public SCard Card { get; }
        }

        private sealed class DefensePlan
        {
            private DefensePlan(bool canDefend, List<DefenseMove> moves, int cost)
            {
                CanDefend = canDefend;
                Moves = moves;
                Cost = cost;
            }

            public bool CanDefend { get; }
            public List<DefenseMove> Moves { get; }
            public int Cost { get; }

            public static DefensePlan Failure()
            {
                return new DefensePlan(false, new List<DefenseMove>(), int.MaxValue);
            }

            public static DefensePlan Success(List<DefenseMove> moves, int cost)
            {
                return new DefensePlan(true, moves, cost);
            }
        }
    }
}
