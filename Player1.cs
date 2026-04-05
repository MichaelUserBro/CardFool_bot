using System;
using System.Collections.Generic;
using System.Linq;

namespace CardFool
{
    // ================================================================
    //  BotConfig — все коэффициенты и константы в одном месте
    // ================================================================

    /// <summary>
    /// Все коэффициенты и константы бота в одном месте.
    /// Значения не изменены, только именованы для читаемости.
    /// </summary>
    public static class BotConfig
    {
        // --- Коэффициенты стадий игры (GetStageCoef) ---
        public const double EarlyGameCoef = 1488;
        public const double MidGameCoef = 9.11;
        public const double LateGameCoef = 67;
        public const double SuperLateCoef = 0.228;

        public const int EarlyGameThreshold = 17; // > 17 карт в колоде — early
        public const int MidGameThreshold = 9;  // 9-16 карт — mid
                                                // 1-8 карт — late
                                                // 0 карт — super late

        // --- Коэффициент желания выбрасывать пары/тройки в конце (GetSaveGameCoef) ---
        public const double SaveGameCoef = 4.0;
        public const double SaveGameCoefDefault = 1.0;
        public const int SaveGameHandThreshold = 4;

        // --- Формула скоринга атаки ---
        public const double AttackNoSuitBase = 2.65;
        public const double AttackThrowNoSuit = 2.65;

        // --- Баффы атаки ---
        public const int PairBuff = 13;
        public const int TrioBuff = 23;

        // --- Бафф за дешевизну хода (GetCheapMoveBuff) ---
        public const double CheapMoveBase = 1.5;
        public const double CheapMoveMult = 30;

        // --- Штраф если противник не может отбить (GetSuccessBuff) ---
        public const double SuccessFailPenalty = -100.0;

        // --- Штраф за последнюю карту масти (GetLastCardPenalty) ---
        public const int LastCardPenaltyValue = 10;
        public const int LastCardRankThreshold = 8;

        // --- Бафф за масть, которой нет у противника (GetNoSuitBuff) ---
        public const int NoSuitBuff = 5;
        public const int NoSuitEnemyThreshold = 3;

        // --- Бафф за кол-во карт на руке (GetCardOnHandBuff) ---
        public const int HandBuff_Small = 30;
        public const int HandBuff_Medium = 1;
        public const int HandBuff_Large = 15;
        public const int HandBuff_XLarge = 30;

        public const int HandThreshold_Small = 6;
        public const int HandThreshold_Medium = 9;
        public const int HandThreshold_Large = 13;

        // --- Скоринг подброса ---
        public const double ThrowScoreBase = 2.5;
        public const double GiveScoreBase = 2.5;

        // --- Пороги решения "делать ход или нет" ---
        public const int ThrowDecisionBoard = 20;
        public const int DefDecisionBoard = 20;

        // --- Скоринг защиты ---
        public const int SuccessfulDefBuff = 30;
        public const double EnemyTrumpPenMult = 0.7;
        public const double DefNoSuitBase = 2.0;

        // --- Бафф оптимальности защиты (GetDefOptimalBuff) ---
        public const int DefOptimalBase = 65;

        // --- Коэффициент разницы карт (GetDifInCardCoef) ---
        public const double DifCoef_Equal = 1.0;
        public const double DifCoef_Small = 1.3;
        public const double DifCoef_Large = 1.8;

        // --- Штраф за козырь у противника (GetEnemyTrumpPenalty) ---
        public const int EnemyTrumpPenValue = 15;

        // --- Бафф/штраф за одинаковые ранги при защите (GetSameRankDefBuff) ---
        public const int SameRankDefBuff = 15;
        public const int DiffRankDefPenalty = -15;

        // --- Штраф за потерю пар (GetLostPairPenalty) ---
        public const int LostPairPenMult = -3;

        // --- Размер колоды ---
        public const int FullDeckSize = 24;
    }

    // ================================================================
    //  GameStateTracker — состояние игры
    // ================================================================

    /// <summary>
    /// Отслеживает состояние игры: карты в руке, колоду, руку противника.
    /// </summary>
    public class GameStateTracker
    {
        /// <summary>Карты на руке бота</summary>
        public List<SCard> Hand { get; private set; } = new List<SCard>();

        /// <summary>Предполагаемые карты противника</summary>
        public List<SCard> OppHand { get; set; } = new List<SCard>();

        /// <summary>Предполагаемые оставшиеся карты в колоде</summary>
        public List<SCard> RemainingDeck { get; private set; } = new List<SCard>();

        /// <summary>Козырная масть</summary>
        public Suits TrumpSuit { get; private set; }

        /// <summary>Количество карт в прикупе</summary>
        public int RemainingDeckCount { get; private set; } = BotConfig.FullDeckSize;

        /// <summary>Количество карт у противника</summary>
        public int OppCardCount { get; set; } = MGameRules.TotalCards;

        /// <summary>Атакует ли бот в текущем раунде</summary>
        public bool IsIAttack { get; set; } = false;

        /// <summary>
        /// Инициализация козыря и полной колоды в начале игры.
        /// </summary>
        /// <param name="trump">Козырная карта</param>
        public void Initialize(SCard trump)
        {
            TrumpSuit = trump.Suit;
            RemainingDeck = MGameRules.GetDeck();
        }

        /// <summary>
        /// Добавить карту в руку бота.
        /// </summary>
        /// <param name="card">Карта для добавления</param>
        public void AddToHand(SCard card)
        {
            Hand.Add(card);
        }

        /// <summary>
        /// Вызывается в конце раунда — обновляет счётчики и списки карт.
        /// </summary>
        /// <param name="table">Карты на столе</param>
        /// <param name="isDefenceSuccessful">Была ли защита успешной</param>
        public void OnEndRound(List<SCardPair> table, bool isDefenceSuccessful)
        {
            UpdateCounts(table, isDefenceSuccessful);
            UpdateCardLists(table, isDefenceSuccessful);
        }

        /// <summary>
        /// Обновляет количество карт у противника и в колоде после раунда.
        /// Логика от Федоса — не трогать.
        /// </summary>
        private void UpdateCounts(List<SCardPair> table, bool isDefenceSuccessful)
        {
            int en1 = isDefenceSuccessful || IsIAttack
                ? Math.Max(0, MGameRules.TotalCards - Hand.Count)
                : 0;

            int en2 = isDefenceSuccessful || !IsIAttack
                ? Math.Max(0, MGameRules.TotalCards - (OppCardCount - table.Count))
                : 0;

            if (RemainingDeckCount >= en1 + en2 && (!IsIAttack || isDefenceSuccessful))
                OppCardCount = Math.Max(MGameRules.TotalCards, OppCardCount - table.Count);
            else if (IsIAttack && !isDefenceSuccessful)
                OppCardCount += table.Count;
            else if (RemainingDeckCount < en1 + en2)
            {
                OppCardCount -= table.Count;
                if (IsIAttack)
                    OppCardCount += en2 != 0 ? Math.Max(0, RemainingDeckCount - en1) : 0;
                else
                    OppCardCount += Math.Min(en2, RemainingDeckCount);
            }

            if (RemainingDeckCount >= en1 + en2)
                RemainingDeckCount -= en1 + en2;
            else
                RemainingDeckCount = 0;
        }

        /// <summary>
        /// Обновляет списки карт (колода и рука противника) после раунда.
        /// </summary>
        private void UpdateCardLists(List<SCardPair> table, bool isDefenceSuccessful)
        {
            List<SCard> cardsOnTable = new List<SCard>();
            for (int i = 0; i < table.Count; i++)
            {
                cardsOnTable.Add(table[i].Down);
                if (table[i].Beaten)
                    cardsOnTable.Add(table[i].Up);
            }

            RemoveCardsFromList(cardsOnTable, RemainingDeck);

            if (IsIAttack && !isDefenceSuccessful)
                OppHand.AddRange(cardsOnTable);
        }

        /// <summary>
        /// Удаляет карты из списка по совпадению Rank и Suit.
        /// </summary>
        /// <param name="toRemove">Карты которые нужно удалить</param>
        /// <param name="source">Список из которого удаляем</param>
        public void RemoveCardsFromList(List<SCard> toRemove, List<SCard> source)
        {
            if (toRemove == null || source == null) return;
            if (toRemove.Count == 0 || source.Count == 0) return;

            for (int i = 0; i < toRemove.Count; i++)
            {
                for (int j = source.Count - 1; j >= 0; j--)
                {
                    if (toRemove[i].Rank == source[j].Rank &&
                        toRemove[i].Suit == source[j].Suit)
                    {
                        source.RemoveAt(j);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Сортировка предполагаемой руки противника:
        /// сначала некозырные по рангу, потом козырные по рангу.
        /// </summary>
        public void SortOppHand()
        {
            OppHand.Sort((a, b) =>
            {
                int aType = (a.Suit == TrumpSuit) ? 1 : 0;
                int bType = (b.Suit == TrumpSuit) ? 1 : 0;
                if (aType != bType) return aType - bType;
                return a.Rank - b.Rank;
            });
        }
    }

    // ================================================================
    //  MoveGenerator — генерация всех возможных ходов
    // ================================================================

    /// <summary>
    /// Генерирует все возможные ходы для атаки, подброса и защиты.
    /// </summary>
    public class MoveGenerator
    {
        private readonly GameStateTracker _state;

        public MoveGenerator(GameStateTracker state)
        {
            _state = state;
        }

        // --- Атака ---

        /// <summary>
        /// Возвращает все возможные ходы для начальной атаки.
        /// Одиночные карты + пары + тройки + четвёрки одного ранга.
        /// </summary>
        /// <returns>Список возможных ходов, каждый ход — список карт</returns>
        public List<List<SCard>> GetAllAttackMoves()
        {
            
            List<SCard> hand = _state.Hand;
            int enemyCount = _state.OppCardCount;
            List<List<SCard>> allMoves = new List<List<SCard>>(20);

            foreach (SCard card in hand)
                allMoves.Add(new List<SCard> { card });

            if (hand.Count > 1 && enemyCount > 1)
                for (int i = 0; i < hand.Count; i++)
                    for (int j = i + 1; j < hand.Count; j++)
                        if (hand[i].Rank == hand[j].Rank)
                            allMoves.Add(new List<SCard> { hand[i], hand[j] });

            if (hand.Count > 2 && enemyCount > 2)
                for (int i = 0; i < hand.Count; i++)
                    for (int j = i + 1; j < hand.Count; j++)
                        for (int k = j + 1; k < hand.Count; k++)
                            if (hand[i].Rank == hand[j].Rank && hand[j].Rank == hand[k].Rank)
                                allMoves.Add(new List<SCard> { hand[i], hand[j], hand[k] });

            if (hand.Count > 3 && enemyCount > 3)
                for (int i = 0; i < hand.Count; i++)
                    for (int j = i + 1; j < hand.Count; j++)
                        for (int k = j + 1; k < hand.Count; k++)
                            for (int l = k + 1; l < hand.Count; l++)
                                if (hand[i].Rank == hand[j].Rank &&
                                    hand[j].Rank == hand[k].Rank &&
                                    hand[k].Rank == hand[l].Rank)
                                    allMoves.Add(new List<SCard> { hand[i], hand[j], hand[k], hand[l] });

            return allMoves;
        }

        // --- Подброс ---

        /// <summary>
        /// Возвращает все возможные ходы для подброса карт на стол.
        /// </summary>
        /// <param name="table">Текущие карты на столе</param>
        /// <param name="limit">Максимальное количество карт для подброса</param>
        /// <returns>Список возможных ходов подброса</returns>
        public List<List<SCard>> GetAllThrowMoves(List<SCardPair> table, int limit)
        {
            List<List<SCard>> allMoves = new List<List<SCard>>();
            if (limit <= 0) return allMoves;

            List<int> ranksOnTable = GetRanksOnTable(table);
            List<SCard> throwable = GetThrowableCards(ranksOnTable);

            if (throwable.Count == 0) return allMoves;

            int maxToThrow = Math.Min(throwable.Count, limit);
            List<SCard> buffer = new List<SCard>(maxToThrow);

            for (int i = 1; i <= maxToThrow; i++)
                GenerateCombinations(throwable, i, 0, buffer, allMoves);

            return allMoves;
        }

        /// <summary>
        /// Собирает уникальные ранги всех карт на столе.
        /// </summary>
        /// <param name="table">Текущие карты на столе</param>
        /// <returns>Список уникальных рангов</returns>
        private List<int> GetRanksOnTable(List<SCardPair> table)
        {
            HashSet<int> ranksSet = new HashSet<int>();

            for (int i = 0; i < table.Count; i++)
            {
                ranksSet.Add(table[i].Down.Rank);
                if (table[i].Beaten)
                    ranksSet.Add(table[i].Up.Rank);
            }

            return new List<int>(ranksSet);
        }

        /// <summary>
        /// Возвращает карты из руки, которые можно подбросить.
        /// </summary>
        /// <param name="ranksOnTable">Ранги карт на столе</param>
        /// <returns>Список карт для подброса</returns>
        private List<SCard> GetThrowableCards(List<int> ranksOnTable)
        {
            List<SCard> throwable = new List<SCard>();

            for (int i = 0; i < _state.Hand.Count; i++)
            {
                SCard card = _state.Hand[i];
                if (!ranksOnTable.Contains(card.Rank)) continue;

                bool alreadyAdded = false;
                for (int j = 0; j < throwable.Count; j++)
                {
                    if (throwable[j].Suit == card.Suit && throwable[j].Rank == card.Rank)
                    {
                        alreadyAdded = true;
                        break;
                    }
                }

                if (!alreadyAdded) throwable.Add(card);
            }

            return throwable;
        }

        /// <summary>
        /// Рекурсивно генерирует все комбинации заданной длины (backtracking).
        /// </summary>
        private void GenerateCombinations(List<SCard> cards, int length, int offset,
            List<SCard> current, List<List<SCard>> allMoves)
        {
            if (length == 0)
            {
                allMoves.Add(new List<SCard>(current));
                return;
            }

            for (int i = offset; i <= cards.Count - length; i++)
            {
                current.Add(cards[i]);
                GenerateCombinations(cards, length - 1, i + 1, current, allMoves);
                current.RemoveAt(current.Count - 1);
            }
        }

        // --- Защита ---

        /// <summary>
        /// Возвращает все возможные комбинации защитных ходов.
        /// </summary>
        /// <param name="table">Текущие карты на столе</param>
        /// <param name="moves">Список найденных защитных ходов (out)</param>
        /// <returns>True если защита возможна, false если нет</returns>
        public bool GetAllDefenceMoves(List<SCardPair> table, out List<List<SCard>> moves)
        {
            moves = new List<List<SCard>>();
            List<HashSet<SCard>> options = new List<HashSet<SCard>>();

            foreach (SCardPair pair in table)
            {
                if (pair.Beaten) continue;

                HashSet<SCard> canBeat = new HashSet<SCard>();
                foreach (SCard card in _state.Hand)
                    if (SCard.CanBeat(pair.Down, card, _state.TrumpSuit))
                        canBeat.Add(card);

                if (canBeat.Count == 0) return false;
                options.Add(canBeat);
            }

            GenerateDefenceCombinations(options, moves);
            return true;
        }

        /// <summary>
        /// Рекурсивно генерирует все комбинации защитных карт.
        /// </summary>
        private void GenerateDefenceCombinations(List<HashSet<SCard>> options, List<List<SCard>> moves)
        {
            List<SCard> current = new List<SCard>();
            GenerateDefenceRecursive(options, current, 0, moves);
        }

        private void GenerateDefenceRecursive(List<HashSet<SCard>> options, List<SCard> current,
        int depth, List<List<SCard>> results)
        {
            if (depth == options.Count)
            {
                results.Add(new List<SCard>(current));
                return;
            }

            foreach (SCard card in options[depth])
            {
                bool alreadyUsed = false;
                for (int i = 0; i < current.Count; i++)
                {
                    if (current[i].Rank == card.Rank && current[i].Suit == card.Suit)
                    {
                        alreadyUsed = true;
                        break;
                    }
                }
                if (alreadyUsed) continue;

                current.Add(card);
                GenerateDefenceRecursive(options, current, depth + 1, results);
                current.RemoveAt(current.Count - 1);
            }
        }
    }

    // ================================================================
    //  Scorer — оценка ходов
    // ================================================================

    /// <summary>
    /// Оценивает ходы бота — атаку, подброс и защиту.
    /// Возвращает числовой score для каждого возможного хода.
    /// </summary>
    public class Scorer
    {
        private readonly GameStateTracker _state;

        public Scorer(GameStateTracker state)
        {
            _state = state;
        }

        // --- Итоговые функции скоринга ---

        /// <summary>
        /// Итоговый score хода при начальной атаке.
        /// </summary>
        /// <param name="attackCards">Карты атакующего хода</param>
        /// <returns>Числовая оценка хода — чем выше, тем лучше</returns>
        public double AttackMoveScore(List<SCard> attackCards)
        {
            double stageCoef = GetStageCoef();
            double saveGameCoef = GetSaveGameCoef();

            int pairBuff = GetPairAndTrioBuff(attackCards);
            double successBuff = GetSuccessBuff(attackCards);
            double cheapBuff = GetCheapMoveBuff(attackCards);
            int noSuitBuff = GetNoSuitBuff(attackCards);
            int trumpPenalty = GetTrumpPenalty(attackCards);
            int lastCardPen = GetLastCardPenalty(attackCards);

            return pairBuff * saveGameCoef
                 + stageCoef * successBuff
                 + stageCoef * cheapBuff
                 + (BotConfig.AttackNoSuitBase - stageCoef) * noSuitBuff
                 + stageCoef * trumpPenalty
                 + stageCoef * lastCardPen;
        }

        /// <summary>
        /// Итоговый score подброса, когда противник берёт карты.
        /// </summary>
        /// <param name="throwCards">Карты для подброса</param>
        /// <returns>Числовая оценка хода — чем выше, тем лучше</returns>
        public double GiveCardScore(List<SCard> throwCards)
        {
            double stageCoef = GetStageCoef();
            int handBuff = GetCardOnHandBuff();
            int priceControl = GetCardPriceControl(throwCards);
            int trumpPenalty = GetTrumpPenalty(throwCards);
            int lastCardPen = GetLastCardPenalty(throwCards);

            return (BotConfig.GiveScoreBase - stageCoef) * handBuff
                 + stageCoef * priceControl
                 + stageCoef * trumpPenalty
                 + stageCoef * lastCardPen;
        }

        /// <summary>
        /// Итоговый score подброса, когда противник отбился.
        /// </summary>
        /// <param name="throwCards">Карты для подброса</param>
        /// <returns>Числовая оценка хода — чем выше, тем лучше</returns>
        public double ThrowCardScore(List<SCard> throwCards)
        {
            double stageCoef = GetStageCoef();
            int handBuff = GetCardOnHandBuff();
            double successBuff = GetSuccessBuff(throwCards);
            int noSuitBuff = GetNoSuitBuff(throwCards);
            int trumpPenalty = GetTrumpPenalty(throwCards);
            int lastCardPen = GetLastCardPenalty(throwCards);

            return (BotConfig.ThrowScoreBase - stageCoef) * handBuff
                 + stageCoef * successBuff
                 + (BotConfig.AttackThrowNoSuit - stageCoef) * noSuitBuff
                 + stageCoef * trumpPenalty
                 + stageCoef * lastCardPen;
        }

        /// <summary>
        /// Итоговый score защитного хода.
        /// </summary>
        /// <param name="move">Карты для защиты</param>
        /// <param name="table">Текущие карты на столе</param>
        /// <returns>Числовая оценка хода — чем выше, тем лучше</returns>
        public double DefMoveScore(List<SCard> move, List<SCardPair> table)
        {
            double stageCoef = GetStageCoef();
            double difCardCoef = GetDifInCardCoef();

            int successDef = BotConfig.SuccessfulDefBuff;
            int defOptimal = GetDefOptimalBuff(move, table);
            int trumpPenalty = GetTrumpPenalty(move);
            int enemyTrumpPen = GetEnemyTrumpPenalty(table);
            int lostPairPen = GetLostPairPenalty(move);
            int sameRankBuff = GetSameRankDefBuff(move);

            return (BotConfig.DefNoSuitBase - stageCoef) * difCardCoef * successDef
                 + stageCoef * defOptimal
                 + stageCoef * trumpPenalty
                 + stageCoef * enemyTrumpPen * BotConfig.EnemyTrumpPenMult
                 + stageCoef * sameRankBuff
                 + lostPairPen;
        }

        // --- Коэффициенты ---

        /// <summary>
        /// Коэффициент стадии игры — зависит от количества карт в колоде.
        /// </summary>
        /// <returns>Коэффициент стадии игры</returns>
        public double GetStageCoef()
        {
            if (_state.RemainingDeckCount == 0)
                return BotConfig.SuperLateCoef;
            if (_state.RemainingDeckCount < BotConfig.MidGameThreshold)
                return BotConfig.LateGameCoef;
            if (_state.RemainingDeckCount < BotConfig.EarlyGameThreshold)
                return BotConfig.MidGameCoef;
            return BotConfig.EarlyGameCoef;
        }

        /// <summary>
        /// Коэффициент желания избавляться от пар и троек в конце игры.
        /// </summary>
        private double GetSaveGameCoef()
        {
            bool isLateGame = _state.RemainingDeckCount == 0;
            bool hasManyMoreCards = (_state.Hand.Count - _state.OppCardCount) > BotConfig.SaveGameHandThreshold;
            return isLateGame && hasManyMoreCards ? BotConfig.SaveGameCoef : BotConfig.SaveGameCoefDefault;
        }

        /// <summary>
        /// Коэффициент разницы в количестве карт между ботом и противником.
        /// </summary>
        private double GetDifInCardCoef()
        {
            int dif = _state.Hand.Count - _state.OppCardCount;
            if (dif <= 0) return BotConfig.DifCoef_Equal;
            if (dif <= 3) return BotConfig.DifCoef_Small;
            return BotConfig.DifCoef_Large;
        }

        // --- Баффы ---

        /// <summary>
        /// Бафф за атаку парой или тройкой одинаковых карт.
        /// </summary>
        private int GetPairAndTrioBuff(List<SCard> cards)
        {
            if (cards.Count == 2) return BotConfig.PairBuff;
            if (cards.Count == 3) return BotConfig.TrioBuff;
            return 0;
        }

        /// <summary>
        /// Бафф за дешевизну хода — чем дешевле карты относительно руки, тем лучше.
        /// </summary>
        private double GetCheapMoveBuff(List<SCard> cards)
        {
            double moveSum = 0;
            double handSum = 0;
            foreach (SCard card in cards) moveSum += card.Rank;
            foreach (SCard card in _state.Hand) handSum += card.Rank;
            return (BotConfig.CheapMoveBase - (moveSum / handSum)) * BotConfig.CheapMoveMult;
        }

        /// <summary>
        /// Бафф за атаку картой масти, которой нет у противника.
        /// </summary>
        private int GetNoSuitBuff(List<SCard> cards)
        {
            if (_state.OppHand.Count <= BotConfig.NoSuitEnemyThreshold &&
                _state.OppCardCount > BotConfig.NoSuitEnemyThreshold)
                return 0;

            int[] suitCounts = new int[4];
            foreach (SCard card in cards)
                if (card.Suit != _state.TrumpSuit)
                    suitCounts[(int)card.Suit]++;

            bool[] oppHasSuit = new bool[4];
            foreach (SCard card in _state.OppHand)
                oppHasSuit[(int)card.Suit] = true;

            int buff = 0;
            for (int suit = 0; suit < 4; suit++)
                if (suitCounts[suit] > 0 && !oppHasSuit[suit])
                    buff += BotConfig.NoSuitBuff * suitCounts[suit];

            return buff;
        }

        /// <summary>
        /// Бафф за мотивацию подкидывать при большом количестве карт на руке.
        /// </summary>
        private int GetCardOnHandBuff()
        {
            if (_state.Hand.Count < BotConfig.HandThreshold_Small) return BotConfig.HandBuff_Small;
            if (_state.Hand.Count < BotConfig.HandThreshold_Medium) return BotConfig.HandBuff_Medium;
            if (_state.Hand.Count < BotConfig.HandThreshold_Large) return BotConfig.HandBuff_Large;
            return BotConfig.HandBuff_XLarge;
        }

        /// <summary>
        /// Бафф за подброс дешёвых карт и штраф за подброс дорогих.
        /// </summary>
        private int GetCardPriceControl(List<SCard> throwCards)
        {
            int score = 0;
            foreach (SCard card in throwCards)
            {
                if (card.Rank <= 9 && card.Suit != _state.TrumpSuit)
                    score += card.Rank;
                else if (card.Rank > 9 && card.Suit != _state.TrumpSuit)
                    score -= card.Rank;
            }
            return score;
        }

        /// <summary>
        /// Симулирует защиту противника и оценивает силу его оставшейся руки.
        /// Чем слабее рука противника после хода — тем выше бафф.
        /// </summary>
        private double GetSuccessBuff(List<SCard> move)
        {
            bool[] used = new bool[_state.OppHand.Count];

            foreach (SCard attackCard in move)
            {
                bool found = false;
                for (int i = 0; i < _state.OppHand.Count; i++)
                {
                    if (!used[i] && SCard.CanBeat(attackCard, _state.OppHand[i], _state.TrumpSuit))
                    {
                        used[i] = true;
                        found = true;
                        break;
                    }
                }

                if (!found) return BotConfig.SuccessFailPenalty;
            }

            double oppPower = 0;
            for (int i = 0; i < _state.OppHand.Count; i++)
            {
                if (used[i]) continue;
                oppPower += _state.OppHand[i].Rank;
                if (_state.OppHand[i].Suit == _state.TrumpSuit) oppPower += 14;
            }

            return -oppPower;
        }

        /// <summary>ы
        /// Бафф за оптимальность защиты — чем меньше тратим на отбитие, тем лучше.
        /// </summary>
        private int GetDefOptimalBuff(List<SCard> move, List<SCardPair> table)
        {
            int difInRank = 0;
            int j = 0;

            for (int i = 0; i < table.Count; i++)
            {
                if (table[i].Beaten) continue;

                SCard down = table[i].Down;
                SCard def = move[j];
                j++;

                if (def.Suit == _state.TrumpSuit && down.Suit != _state.TrumpSuit)
                    difInRank += def.Rank + (14 - down.Rank);
                else
                    difInRank += def.Rank - down.Rank;
            }

            return BotConfig.DefOptimalBase - difInRank;
        }

        /// <summary>
        /// Бафф/штраф за количество одинаковых рангов в защитном ходе.
        /// </summary>
        private int GetSameRankDefBuff(List<SCard> move)
        {
            HashSet<int> uniqueRanks = new HashSet<int>();
            foreach (SCard card in move) uniqueRanks.Add(card.Rank);

            if (uniqueRanks.Count == 1) return BotConfig.SameRankDefBuff;
            if (uniqueRanks.Count == 2) return 0;
            return BotConfig.DiffRankDefPenalty;
        }

        // --- Штрафы ---

        /// <summary>
        /// Штраф за использование козырей в ходе.
        /// </summary>
        private int GetTrumpPenalty(List<SCard> cards)
        {
            int penalty = 0;
            foreach (SCard card in cards)
                if (card.Suit == _state.TrumpSuit)
                    penalty += card.Rank;
            return -penalty;
        }

        /// <summary>
        /// Штраф за использование последней карты данной масти при ходе старшими картами.
        /// </summary>
        private int GetLastCardPenalty(List<SCard> cards)
        {
            int penalty = 0;
            foreach (SCard card in cards)
            {
                if (card.Rank <= BotConfig.LastCardRankThreshold) continue;

                int suitCount = 0;
                foreach (SCard handCard in _state.Hand)
                    if (handCard.Suit == card.Suit)
                        suitCount++;

                if (suitCount == 1)
                    penalty += BotConfig.LastCardPenaltyValue;
            }
            return -penalty;
        }

        /// <summary>
        /// Штраф за то, что противник атакует козырями.
        /// </summary>
        private int GetEnemyTrumpPenalty(List<SCardPair> table)
        {
            int penalty = 0;
            foreach (SCardPair pair in table)
                if (pair.Down.Suit == _state.TrumpSuit)
                    penalty += BotConfig.EnemyTrumpPenValue;
            return -penalty;
        }

        /// <summary>
        /// Штраф за потерю пар и троек при защите.
        /// </summary>
        private int GetLostPairPenalty(List<SCard> move)
        {
            int count = -move.Count;
            foreach (SCard defCard in move)
                foreach (SCard handCard in _state.Hand)
                    if (defCard.Rank == handCard.Rank)
                        count++;
            return BotConfig.LostPairPenMult * count;
        }
    }

    // ================================================================
    //  MPlayer1 — публичный интерфейс бота
    // ================================================================

    public class MPlayer1
    {
        private readonly string _name = "ЕКБ 3.1";
        private readonly GameStateTracker _state = new GameStateTracker();
        private readonly MoveGenerator _generator;
        private readonly Scorer _scorer;

        public MPlayer1()
        {
            _generator = new MoveGenerator(_state);
            _scorer = new Scorer(_state);
        }

        // --- Обязательный интерфейс ---

        /// <summary>Возвращает имя бота</summary>
        public string GetName() => _name;

        /// <summary>Возвращает количество карт на руке</summary>
        public int GetCount() => _state.Hand.Count;

        /// <summary>Добавляет карту в руку бота</summary>
        /// <param name="card">Карта для добавления</param>
        public void AddToHand(SCard card) => _state.AddToHand(card);

        /// <summary>
        /// Инициализация козыря перед первой раздачей.
        /// </summary>
        /// <param name="newTrump">Козырная карта</param>
        public void SetTrump(SCard newTrump) => _state.Initialize(newTrump);

        /// <summary>
        /// Вызывается в конце раунда — обновляет состояние игры.
        /// </summary>
        /// <param name="table">Карты на столе</param>
        /// <param name="isDefenceSuccessful">Была ли защита успешной</param>
        public void OnEndRound(List<SCardPair> table, bool isDefenceSuccessful)
        {
            _state.OnEndRound(table, isDefenceSuccessful);
        }

        /// <summary>
        /// Начальная атака — выбирает лучший ход и выкладывает карты.
        /// </summary>
        /// <returns>Список карт для атаки</returns>
        public List<SCard> LayCards()
        {
            _state.RemoveCardsFromList(_state.Hand, _state.RemainingDeck);
            _state.IsIAttack = true;

            List<List<SCard>> allMoves = _generator.GetAllAttackMoves();
            List<SCard> best = ChooseBestMove(allMoves, _scorer.AttackMoveScore);

            foreach (SCard card in best)
                _state.Hand.Remove(card);

            return best;
        }

        /// <summary>
        /// Подброс карт на стол.
        /// </summary>
        /// <param name="table">Текущие карты на столе</param>
        /// <param name="opponentDefenced">Отбился ли противник</param>
        /// <returns>True если подбросили карты, false если нет</returns>
        public bool AddCards(List<SCardPair> table, bool opponentDefenced)
        {
            int limit = Math.Min(MGameRules.TotalCards - table.Count,
                                 _state.OppCardCount - table.Count);
            if (limit <= 0) return false;

            List<List<SCard>> moves = _generator.GetAllThrowMoves(table, limit);
            if (moves.Count == 0) return false;

            List<SCard> best;
            bool willThrow;

            if (opponentDefenced)
                willThrow = ChooseThrowMove(moves, _scorer.ThrowCardScore, out best);
            else
                willThrow = ChooseThrowMove(moves, _scorer.GiveCardScore, out best);

            if (!willThrow) return false;

            foreach (SCard card in best)
            {
                table.Add(new SCardPair(card));
                _state.Hand.Remove(card);
            }

            return true;
        }

        /// <summary>
        /// Защита от карт противника.
        /// </summary>
        /// <param name="table">Текущие карты на столе</param>
        /// <returns>True если отбился, false если берёт карты</returns>
        public bool Defend(List<SCardPair> table)
        {
            List<SCard> attackCards = new List<SCard>();
            for (int i = 0; i < table.Count; i++)
                attackCards.Add(table[i].Down);

            _state.RemoveCardsFromList(attackCards, _state.OppHand);
            _state.IsIAttack = false;

            bool canDefend = _generator.GetAllDefenceMoves(table, out List<List<SCard>> moves);
            if (!canDefend) return false;

            bool willDefend = ChooseDefMove(moves, table, out List<SCard> best);
            if (!willDefend) return false;

            int j = 0;
            for (int i = 0; i < table.Count; i++)
            {
                if (!table[i].Beaten)
                {
                    var pair = table[i];
                    pair.SetUp(best[j], _state.TrumpSuit);
                    table[i] = pair;
                    _state.Hand.Remove(best[j]);
                    j++;
                }
            }

            return true;
        }

        // --- Выбор лучшего хода ---

        /// <summary>
        /// Выбирает ход с максимальным score из списка ходов.
        /// </summary>
        /// <param name="moves">Список возможных ходов</param>
        /// <param name="scoreFunc">Функция оценки хода</param>
        /// <returns>Лучший ход</returns>
        private List<SCard> ChooseBestMove(List<List<SCard>> moves, Func<List<SCard>, double> scoreFunc)
        {
            List<SCard> best = moves[0];
            double bestScore = scoreFunc(moves[0]);

            foreach (List<SCard> move in moves)
            {
                double score = scoreFunc(move);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = move;
                }
            }

            return best;
        }

        /// <summary>
        /// Выбирает лучший ход для подброса — только если score выше порога.
        /// </summary>
        /// <param name="moves">Список возможных ходов</param>
        /// <param name="scoreFunc">Функция оценки хода</param>
        /// <param name="best">Выбранный ход (out)</param>
        /// <returns>True если стоит подбрасывать, false если нет</returns>
        private bool ChooseThrowMove(List<List<SCard>> moves,
        Func<List<SCard>, double> scoreFunc, out List<SCard> best)
        {
            best = new List<SCard>();
            double bestScore = 0;

            foreach (List<SCard> move in moves)
            {
                bool allInHand = true;
                for (int i = 0; i < move.Count; i++)
                {
                    bool found = false;
                    for (int j = 0; j < _state.Hand.Count; j++)
                    {
                        if (_state.Hand[j].Rank == move[i].Rank &&
                            _state.Hand[j].Suit == move[i].Suit)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found) { allInHand = false; break; }
                }

                if (!allInHand) continue;

                double score = scoreFunc(move);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = move;
                }
            }

            return bestScore > BotConfig.ThrowDecisionBoard;
        }

        /// <summary>
        /// Выбирает лучший защитный ход — только если score выше порога.
        /// </summary>
        /// <param name="moves">Список возможных ходов</param>
        /// <param name="table">Текущие карты на столе</param>
        /// <param name="best">Выбранный ход (out)</param>
        /// <returns>True если стоит защищаться, false если лучше взять карты</returns>
        private bool ChooseDefMove(List<List<SCard>> moves, List<SCardPair> table, out List<SCard> best)
        {
            best = new List<SCard>();
            double bestScore = 0;

            foreach (List<SCard> move in moves)
            {
                double score = _scorer.DefMoveScore(move, table);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = move;
                }
            }

            return bestScore > BotConfig.DefDecisionBoard;
        }
    }

    // ================================================================
    #region Archive
    // ================================================================
    // Старые версии методов до рефакторинга.
    // Регион можно свернуть в IDE.
    // ================================================================

    // --- Из MPlayer1 ---

    // GetStageCoef (логика без изменений, перенесена в Scorer)
    // private double GetStageCoef()
    // {
    //     if (remainingDeckCount == 0) return 0.228;   // super late game
    //     if (remainingDeckCount < 9)  return 67;      // late game
    //     if (remainingDeckCount < 17) return 9.11;    // mid game
    //     return 1488;                                  // early game
    // }

    // GetPairAndTrioBuff (переименован в GetPairAndTrioBuff, перенесён в Scorer)
    // private int GetPairAndTrioBuff(List<SCard> cards)
    // {
    //     if (cards.Count == 2) return 13;
    //     if (cards.Count == 3) return 23;
    //     return 0;
    // }

    // GetSuccessBuff (логика без изменений, перенесена в Scorer.GetSuccessBuff)
    // private double GetSuccessBuff(List<SCard> move) { ... }

    // GetEnemyChanceToDeff (удалена как неиспользуемая)
    // private double GetEnemyChanceToDeff(List<SCard> attCards) { ... }

    // GetTrumpPenalty (логика без изменений, перенесена в Scorer)
    // private int GetTrumpPenalty(List<SCard> cards) { ... }

    // GetLastCardPenalty (логика без изменений, перенесена в Scorer)
    // private int GetLastCardPenalty(List<SCard> cards) { ... }

    // GetChaepMoveBuff (переименован в GetCheapMoveBuff, перенесён в Scorer)
    // private double GetChaepMoveBuff(List<SCard> cards) { ... }

    // GetSAveGameCoef (переименован в GetSaveGameCoef, перенесён в Scorer)
    // private double GetSAveGameCoef() { ... }

    // GetNoSuitCard (переименован в GetNoSuitBuff, перенесён в Scorer)
    // private int GetNoSuitCard(List<SCard> attCards) { ... }

    // GetCardOnHandBuff (логика без изменений, перенесена в Scorer)
    // private int GetCardOnHandBuff() { ... }

    // GetCardPriceControl (логика без изменений, перенесена в Scorer)
    // private int GetCardPriceControl(List<SCard> throw_cards) { ... }

    // GetDifInCardCoef (логика без изменений, перенесена в Scorer)
    // private double GetDifInCardCoef() { ... }

    // GetEnemyTrumpPenalty (логика без изменений, перенесена в Scorer)
    // private int GetEnemyTrumpPenalty(List<SCardPair> table) { ... }

    // GetCardsToDefCount (переименован в GetSameRankDefBuff, перенесён в Scorer)
    // private int GetCardsToDefCount(List<SCard> move) { ... }

    // GetDifInCardRank (переименован в GetDefOptimalBuff, перенесён в Scorer)
    // private int GetDifInCardRank(List<SCard> move, List<SCardPair> table) { ... }

    // GetLostPairPenalty (логика без изменений, перенесена в Scorer)
    // private int GetLostPairPenalty(List<SCard> move) { ... }

    // CompareCards (перенесён внутрь GameStateTracker.SortOppHand как лямбда)
    // private int CompareCards(SCard a, SCard b, Suits trumpSuit) { ... }

    // ChangeListOfCard (переименован в UpdateCardLists, перенесён в GameStateTracker)
    // private void ChangeListOfCard(List<SCardPair> table, bool IsDefenceSuccesful) { ... }

    // DeleteFromCardList (переименован в RemoveCardsFromList, перенесён в GameStateTracker)
    // private void DeleteFromCardList(List<SCard> cardsForDelete, List<SCard> cardsFromDelete) { ... }

    // ChangeCountsOfCard (переименован в UpdateCounts, перенесён в GameStateTracker)
    // private void ChangeCountsOfCard(List<SCardPair> table, bool IsDefenceSuccesful) { ... }

    // FindAllAttackMoves (переименован в GetAllAttackMoves, перенесён в MoveGenerator)
    // private List<List<SCard>> FindAllAttackMoves() { ... }

    // FindAllThrowMove (переименован в GetAllThrowMoves, перенесён в MoveGenerator)
    // private List<List<SCard>> FindAllThrowMove(List<SCardPair> table, int limit) { ... }

    // Gen (переименован в GenerateCombinations, перенесён в MoveGenerator)
    // private void Gen(...) { ... }

    // FindAllDefMoves (переименован в GetAllDefenceMoves, перенесён в MoveGenerator)
    // public bool FindAllDefMoves(List<SCardPair> table, List<List<SCard>> moves) { ... }

    // GenerateCombinations (переименован в GenerateDefenceCombinations, перенесён в MoveGenerator)
    // public static void GenerateCombinations(...) { ... }

    // GenerateRecursive (переименован в GenerateDefenceRecursive, перенесён в MoveGenerator)
    // private static void GenerateRecursive(...) { ... }

    #endregion
}