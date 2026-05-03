using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;

namespace CardFool
{
    public class MPlayer1
    {
        #region Константы
        // --- Коэффициенты стадий игры (GetStageCoef) — АТАКА ---
        public const double Attack_EarlyGameCoef = 839.1721;
        public const double Attack_MidGameCoef = 13.4002;
        public const double Attack_LateGameCoef = 16.822;
        public const double Attack_SuperLateCoef = 4.696;
        public const int Attack_EarlyGameThreshold = 22;
        public const int Attack_MidGameThreshold = 9;

        // --- Коэффициенты стадий игры (GetStageCoef) — ПОДБРОС (при отбитии) ---
        public const double Throw_EarlyGameCoef = 2248.7449;
        public const double Throw_MidGameCoef = 13.3009;
        public const double Throw_LateGameCoef = 2.5532;
        public const double Throw_SuperLateCoef = 1.3101;
        public const int Throw_EarlyGameThreshold = 19;
        public const int Throw_MidGameThreshold = 6;

        // --- Коэффициенты стадий игры (GetStageCoef) — ПОДБРОС (при взятии) ---
        public const double Give_EarlyGameCoef = 401.6459;
        public const double Give_MidGameCoef = 32.6968;
        public const double Give_LateGameCoef = 264.1807;
        public const double Give_SuperLateCoef = 2.2421;
        public const int Give_EarlyGameThreshold = 21;
        public const int Give_MidGameThreshold = 8;

        // --- Коэффициенты стадий игры (GetStageCoef) — ЗАЩИТА ---
        public const double Def_EarlyGameCoef = 468.9455;
        public const double Def_MidGameCoef = 298.9388;
        public const double Def_LateGameCoef = 36.5396;
        public const double Def_SuperLateCoef = 8.1919;
        public const int Def_EarlyGameThreshold = 17;
        public const int Def_MidGameThreshold = 6;

        // --- Коэффициент желания выбрасывать пары/тройки в конце (GetSaveGameCoef) ---
        public const double SaveGameCoef = 9.9686;
        public const double SaveGameCoefDefault = 2.7554;
        public const int SaveGameHandThreshold = 7;

        // --- Формула скоринга атаки ---
        public const double AttackNoSuitBase = 16.3486;
        public const double AttackThrowNoSuit = 5.4152;

        // --- Баффы атаки ---
        public const int PairBuff = 75;
        public const int TrioBuff = 40;

        // --- Бафф за дешевизну хода (GetCheapMoveBuff) ---
        public const double CheapMoveBase = 0.5852;
        public const double CheapMoveMult = 63.6646;

        // --- Штраф если противник не может отбить (GetSuccessBuff) — АТАКА ---
        public const double Attack_SuccessFailPenalty = -100.0;
        public const double Attack_SuccessBuffMult = 0.1;

        // --- Штраф если противник не может отбить (GetSuccessBuff) — ПОДБРОС (при отбитии) ---
        public const double Throw_SuccessFailPenalty = -100.0;
        public const double Throw_SuccessBuffMult = 0.6558;

        // --- Штраф за последнюю карту масти (GetLastCardPenalty) — АТАКА ---
        public const int Attack_LastCardPenaltyValue = 4;
        public const int Attack_LastCardRankThreshold = 9;

        // --- Штраф за последнюю карту масти (GetLastCardPenalty) — ПОДБРОС (при отбитии) ---
        public const int Throw_LastCardPenaltyValue = 1;
        public const int Throw_LastCardRankThreshold = 7;

        // --- Штраф за последнюю карту масти (GetLastCardPenalty) — ПОДБРОС (при взятии) ---
        public const int Give_LastCardPenaltyValue = 18;
        public const int Give_LastCardRankThreshold = 11;

        // --- Бафф за масть, которой нет у противника (GetNoSuitBuff) ---
        public const int NoSuitBuff = 12;
        public const int NoSuitEnemyThreshold = 2;

        // --- Бафф за кол-во карт на руке (GetCardOnHandBuff) ---
        public const int HandBuff_Small = 179;
        public const int HandBuff_Medium = 43;
        public const int HandBuff_Large = 57;
        public const int HandBuff_XLarge = 137;

        public const int HandThreshold_Small = 8;
        public const int HandThreshold_Medium = 9;
        public const int HandThreshold_Large = 11;

        // --- Скоринг подброса ---
        public const double ThrowScoreBase = 20.7753;
        public const double GiveScoreBase = 23.4861;
        public const double GiveWillingnessMult = 1.1647;

        // --- Пороги решения "делать ход или нет" ---
        public const int ThrowDecisionBoard = 16;
        public const int DefDecisionBoard = 42;

        // --- Скоринг защиты ---
        public const int SuccessfulDefBuff = 70;
        public const double EnemyTrumpPenMult = 2.52;
        public const double DefNoSuitBase = 11.7657;

        // --- Бафф оптимальности защиты (GetDefOptimalBuff) ---
        public const int DefOptimalBase = 168;

        // --- Коэффициент разницы карт (GetDifInCardCoef) ---
        public const double DifCoef_Equal = 1.9377;
        public const double DifCoef_Small = 1.944;
        public const double DifCoef_Large = 2.0343;

        // --- Штраф за козырь у противника (GetEnemyTrumpPenalty) ---
        public const int EnemyTrumpPenValue = 20;

        // --- Множители штрафа за козырь (TrumpPenalty) по контекстам ---
        public const double Attack_TrumpPenaltyMult = 4.476;
        public const double Throw_TrumpPenaltyMult = 3.8778;
        public const double Give_TrumpPenaltyMult = 1.2358;
        public const double Def_TrumpPenaltyMult = 0.9852;

        // --- Бафф/штраф за одинаковые ранги при защите (GetSameRankDefBuff) ---
        public const int SameRankDefBuff = 4;
        public const int DiffRankDefPenalty = -47;

        // --- Штраф за потерю пар (GetLostPairPenalty) ---
        public const double LostPairPenMult = -21.1355;

        // --- Размер колоды ---
        public const int FullDeckSize = 24;
        #endregion

        #region Отслеживание состояния игры
        public List<SCard> Hand = new List<SCard>();            // Рука бота
        public List<SCard> OppHandKnown = new List<SCard>();    // Известная рука противника
        public List<SCard> OppHandPossible = new List<SCard>(); // Что может быть в руке противника
        public List<SCard> RemainingDeck = new List<SCard>();   // Карты в прикупе
        public Suits TrumpSuit;                                 // Масть козыря
        public int RemainingDeckCount = FullDeckSize;           // Сколько карт осталось в прикупе
        public int OppCardCount = MGameRules.TotalCards;        // 
        public bool IsBotAttack = false;                        // Отследивание кто сейчас атакует

        // Инициализация козыря, колоды и руки противника
        public void Initialize(SCard trump)
        {
            TrumpSuit = trump.Suit;
            RemainingDeck = MGameRules.GetDeck();
            OppHandPossible = MGameRules.GetDeck();
        }

        // Добавление карты в руку
        public void AddToHand(SCard card)
        {
            Hand.Add(card);
            List<SCard> temp = new List<SCard> { card };
            RemoveCardsFromList(temp, OppHandPossible);
        }

        // Действия по окончанию раунда
        public void OnEndRound(List<SCardPair> table, bool isDefenceSuccessful)
        {
            UpdateCounts(table, isDefenceSuccessful);
            UpdateCardLists(table, isDefenceSuccessful);
        }

        // Обновление счётчиков карт
        private void UpdateCounts(List<SCardPair> table, bool isDefenceSuccessful)
        {
            int en1 = isDefenceSuccessful || IsBotAttack
                ? Math.Max(0, MGameRules.TotalCards - Hand.Count)
                : 0;

            int en2 = isDefenceSuccessful || !IsBotAttack
                ? Math.Max(0, MGameRules.TotalCards - (OppCardCount - table.Count))
                : 0;

            if (RemainingDeckCount >= en1 + en2 && (!IsBotAttack || isDefenceSuccessful))
                OppCardCount = Math.Max(MGameRules.TotalCards, OppCardCount - table.Count);
            else if (IsBotAttack && !isDefenceSuccessful)
                OppCardCount += table.Count;
            else if (RemainingDeckCount < en1 + en2)
            {
                OppCardCount -= table.Count;
                if (IsBotAttack)
                    OppCardCount += en2 != 0 ? Math.Max(0, RemainingDeckCount - en1) : 0;
                else
                    OppCardCount += Math.Min(en2, RemainingDeckCount);
            }

            if (RemainingDeckCount >= en1 + en2)
                RemainingDeckCount -= en1 + en2;
            else
                RemainingDeckCount = 0;
        }

        // Обновление списков карт
        private void UpdateCardLists(List<SCardPair> table, bool isDefenceSuccessful)
        {
            if (IsBotAttack)
            {
                if (isDefenceSuccessful)
                {
                    // Противник отбился — убираем его карты защиты из Known и Possible
                    for (int i = 0; i < table.Count; i++)
                    {
                        if (!table[i].Beaten) continue;
                        List<SCard> played = new List<SCard> { table[i].Up };
                        RemoveCardsFromList(played, OppHandKnown);
                        RemoveCardsFromList(played, OppHandPossible);
                    }
                }
                else
                {
                    // Противник взял все карты — добавляем в Known только если там ещё нет
                    for (int i = 0; i < table.Count; i++)
                    {
                        SCard down = table[i].Down;
                        bool alreadyKnown = false;
                        for (int j = 0; j < OppHandKnown.Count; j++)
                            if (OppHandKnown[j].Rank == down.Rank && OppHandKnown[j].Suit == down.Suit)
                            { alreadyKnown = true; break; }
                        if (!alreadyKnown) OppHandKnown.Add(down);

                        if (table[i].Beaten)
                        {
                            SCard up = table[i].Up;
                            bool alreadyKnownUp = false;
                            for (int j = 0; j < OppHandKnown.Count; j++)
                                if (OppHandKnown[j].Rank == up.Rank && OppHandKnown[j].Suit == up.Suit)
                                { alreadyKnownUp = true; break; }
                            if (!alreadyKnownUp) OppHandKnown.Add(up);
                        }
                    }
                    RemoveCardsFromList(OppHandKnown, OppHandPossible);
                }
            }
            else
            {
                // Противник атаковал — убираем его карты атаки из Known и Possible
                for (int i = 0; i < table.Count; i++)
                {
                    List<SCard> played = new List<SCard> { table[i].Down };
                    RemoveCardsFromList(played, OppHandKnown);
                    RemoveCardsFromList(played, OppHandPossible);
                }
            }


            List<SCard> cardsOnTable = new List<SCard>();
            for (int i = 0; i < table.Count; i++)
            {
                cardsOnTable.Add(table[i].Down);
                if (table[i].Beaten)
                    cardsOnTable.Add(table[i].Up);
            }
            RemoveCardsFromList(cardsOnTable, RemainingDeck);
        }

        // Удаление карт из списка
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
        #endregion

        #region Генерация ходов

        // Создание списка всех возможных ходов для атаки
        public List<List<SCard>> GetAllAttackMoves()
        {

            List<SCard> hand = Hand;
            int enemyCount = OppCardCount;
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

        //
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

        //
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

        //
        private List<SCard> GetThrowableCards(List<int> ranksOnTable)
        {
            List<SCard> throwable = new List<SCard>();

            for (int i = 0; i < Hand.Count; i++)
            {
                SCard card = Hand[i];
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

        //
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

        //
        public bool GetAllDefenceMoves(List<SCardPair> table, out List<List<SCard>> moves)
        {
            moves = new List<List<SCard>>();
            List<HashSet<SCard>> options = new List<HashSet<SCard>>();

            foreach (SCardPair pair in table)
            {
                if (pair.Beaten) continue;

                HashSet<SCard> canBeat = new HashSet<SCard>();
                foreach (SCard card in Hand)
                    if (SCard.CanBeat(pair.Down, card, TrumpSuit))
                        canBeat.Add(card);

                if (canBeat.Count == 0) return false;
                options.Add(canBeat);
            }

            GenerateDefenceCombinations(options, moves);
            return true;
        }

        //
        private void GenerateDefenceCombinations(List<HashSet<SCard>> options, List<List<SCard>> moves)
        {
            List<SCard> current = new List<SCard>();
            GenerateDefenceRecursive(options, current, 0, moves);
        }

        //
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
        #endregion

        #region Функции оценки

        // Оценка атаки
        public double AttackMoveScore(List<SCard> attackCards)
        {
            double stageCoef = GetAttackStageCoef();
            double saveGameCoef = GetSaveGameCoef();

            int pairBuff = GetPairAndTrioBuff(attackCards);
            double successBuff = GetAttackSuccessBuff(attackCards);
            double cheapBuff = GetCheapMoveBuff(attackCards);
            double noSuitBuff = GetNoSuitBuff(attackCards);
            int trumpPenalty = GetAttackTrumpPenalty(attackCards);
            int lastCardPen = GetAttackLastCardPenalty(attackCards);

            return pairBuff * saveGameCoef
                 + stageCoef * Attack_SuccessBuffMult * successBuff
                 + stageCoef * cheapBuff
                 + (AttackNoSuitBase - stageCoef) * noSuitBuff
                 + stageCoef * Attack_TrumpPenaltyMult * trumpPenalty
                 + stageCoef * lastCardPen;
        }

        // Оценка подброса, если пртивник взял
        public double GiveCardScore(List<SCard> throwCards)
        {
            double stageCoef = GetGiveStageCoef();
            int cardDiff = Hand.Count - OppCardCount;
            double throwWillingness = cardDiff * GiveWillingnessMult * stageCoef;

            int priceControl = GetCardPriceControl(throwCards);
            int trumpPenalty = GetGiveTrumpPenalty(throwCards);
            int lastCardPen = GetGiveLastCardPenalty(throwCards);

            return throwWillingness
                 + stageCoef * priceControl
                 + stageCoef * Give_TrumpPenaltyMult * trumpPenalty
                 + stageCoef * lastCardPen;
        }

        // Оценка подброса, если противник отбился
        public double ThrowCardScore(List<SCard> throwCards)
        {
            double stageCoef = GetThrowStageCoef();
            int handBuff = GetCardOnHandBuff();
            double successBuff = GetThrowSuccessBuff(throwCards);
            double noSuitBuff = GetNoSuitBuff(throwCards);
            int trumpPenalty = GetThrowTrumpPenalty(throwCards);
            int lastCardPen = GetThrowLastCardPenalty(throwCards);

            return (ThrowScoreBase - stageCoef) * handBuff
                 + stageCoef * Throw_SuccessBuffMult * successBuff
                 + (AttackThrowNoSuit - stageCoef) * noSuitBuff
                 + stageCoef * Throw_TrumpPenaltyMult * trumpPenalty
                 + stageCoef * lastCardPen;
        }

        // Оценка защиты
        public double DefMoveScore(List<SCard> move, List<SCardPair> table)
        {
            double stageCoef = GetDefStageCoef();
            double difCardCoef = GetDifInCardCoef();
            double lostPairPen = GetLostPairPenalty(move);

            int successDef = SuccessfulDefBuff;
            int defOptimal = GetDefOptimalBuff(move, table);
            int trumpPenalty = GetDefTrumpPenalty(move);
            int enemyTrumpPen = GetEnemyTrumpPenalty(table);
            int sameRankBuff = GetSameRankDefBuff(move);

            return (DefNoSuitBase - stageCoef) * difCardCoef * successDef
                 + stageCoef * defOptimal
                 + stageCoef * Def_TrumpPenaltyMult * trumpPenalty
                 + stageCoef * enemyTrumpPen * EnemyTrumpPenMult
                 + stageCoef * sameRankBuff
                 + lostPairPen;
        }

        // Стадия игры в атаке
        public double GetAttackStageCoef()
        {
            if (RemainingDeckCount == 0)
                return Attack_SuperLateCoef;
            if (RemainingDeckCount < Attack_MidGameThreshold)
                return Attack_LateGameCoef;
            if (RemainingDeckCount < Attack_EarlyGameThreshold)
                return Attack_MidGameCoef;
            return Attack_EarlyGameCoef;
        }

        // Стадия игры при подбросе, если противник отбился
        public double GetThrowStageCoef()
        {
            if (RemainingDeckCount == 0)
                return Throw_SuperLateCoef;
            if (RemainingDeckCount < Throw_MidGameThreshold)
                return Throw_LateGameCoef;
            if (RemainingDeckCount < Throw_EarlyGameThreshold)
                return Throw_MidGameCoef;
            return Throw_EarlyGameCoef;
        }

        // Стадия игры при подбросе, если противник взял
        public double GetGiveStageCoef()
        {
            if (RemainingDeckCount == 0)
                return Give_SuperLateCoef;
            if (RemainingDeckCount < Give_MidGameThreshold)
                return Give_LateGameCoef;
            if (RemainingDeckCount < Give_EarlyGameThreshold)
                return Give_MidGameCoef;
            return Give_EarlyGameCoef;
        }

        // Стадия игры при защите
        public double GetDefStageCoef()
        {
            if (RemainingDeckCount == 0)
                return Def_SuperLateCoef;
            if (RemainingDeckCount < Def_MidGameThreshold)
                return Def_LateGameCoef;
            if (RemainingDeckCount < Def_EarlyGameThreshold)
                return Def_MidGameCoef;
            return Def_EarlyGameCoef;
        }

        // Коэффициент увелечения желания подбрасывать, если большая разница карт в эндгейме (спасательный круг)
        private double GetSaveGameCoef()
        {
            bool isLateGame = RemainingDeckCount == 0;
            bool hasManyMoreCards = (Hand.Count - OppCardCount) > SaveGameHandThreshold;
            return isLateGame && hasManyMoreCards ? SaveGameCoef : SaveGameCoefDefault;
        }

        //
        private double GetDifInCardCoef()
        {
            int dif = Hand.Count - OppCardCount;
            if (dif <= 0) return DifCoef_Equal;
            if (dif <= 3) return DifCoef_Small;
            return DifCoef_Large;
        }

        // Бафф за использование пар и троек
        private int GetPairAndTrioBuff(List<SCard> cards)
        {
            if (cards.Count == 2) return PairBuff;
            if (cards.Count == 3) return TrioBuff;
            return 0;
        }

        // Бафф за использование дешевых карт в атаке
        private double GetCheapMoveBuff(List<SCard> cards)
        {
            double moveSum = 0;
            double handSum = 0;
            foreach (SCard card in cards) moveSum += card.Rank;
            foreach (SCard card in Hand) handSum += card.Rank;
            return (CheapMoveBase - (moveSum / handSum)) * CheapMoveMult;
        }

        // Бафф за атаку мастью, которой нет у противника
        private double GetNoSuitBuff(List<SCard> cards)
        {
            double buff = 0;

            int[] suitCounts = new int[4];
            foreach (SCard card in cards)
                if (card.Suit != TrumpSuit)
                    suitCounts[(int)card.Suit]++;

            if (OppHandKnown.Count > NoSuitEnemyThreshold &&
                OppCardCount < NoSuitEnemyThreshold)
            {

                bool[] oppHasSuit = new bool[4];
                foreach (SCard card in OppHandKnown)
                    oppHasSuit[(int)card.Suit] = true;

                for (int suit = 0; suit < 4; suit++)
                    if (suitCounts[suit] > 0 && !oppHasSuit[suit])
                        buff += NoSuitBuff * suitCounts[suit];

                return buff;
            }

            else
            {
                int[] oppHasSuit = new int[4];
                foreach (SCard card in OppHandPossible)
                    oppHasSuit[(int)card.Suit] += 1;

                if (OppHandPossible.Count == 0) return buff;

                for (int suit = 0; suit < 4; suit++)
                    if (suitCounts[suit] > 0)
                    {
                        double prob = 1.0 - (double)oppHasSuit[suit] / OppHandPossible.Count;
                        buff += NoSuitBuff * suitCounts[suit] * prob;
                    }

                return buff;

            }
        }

        //
        private int GetCardOnHandBuff()
        {
            if (Hand.Count < HandThreshold_Small) return HandBuff_Small;
            if (Hand.Count < HandThreshold_Medium) return HandBuff_Medium;
            if (Hand.Count < HandThreshold_Large) return HandBuff_Large;
            return HandBuff_XLarge;
        }

        // 
        private int GetCardPriceControl(List<SCard> throwCards)
        {

            int score = 0;
            foreach (SCard card in throwCards)
            {

                int count = 0;
                for (int i = 0; i < OppHandKnown.Count; i++)
                    if (OppHandKnown[i].Rank == card.Rank) count++;

                if (count > 0) score -= card.Rank * count;
                else if (card.Rank <= 9 && card.Suit != TrumpSuit)
                    score += card.Rank;
                else if (card.Rank > 9 && card.Suit != TrumpSuit)
                    score -= card.Rank;
            }

            return score;
        }

        // 
        private double GetAttackSuccessBuff(List<SCard> move)
        {
            bool[] used = new bool[OppHandKnown.Count];

            foreach (SCard attackCard in move)
            {
                bool found = false;
                for (int i = 0; i < OppHandKnown.Count; i++)
                {
                    if (!used[i] && SCard.CanBeat(attackCard, OppHandKnown[i], TrumpSuit))
                    {
                        used[i] = true;
                        found = true;
                        break;
                    }
                }

                if (!found) return Attack_SuccessFailPenalty;
            }

            double oppPower = 0;
            for (int i = 0; i < OppHandKnown.Count; i++)
            {
                if (used[i]) continue;
                oppPower += OppHandKnown[i].Rank;
                if (OppHandKnown[i].Suit == TrumpSuit) oppPower += 14;
            }

            return -oppPower;
        }

        //
        private double GetThrowSuccessBuff(List<SCard> move)
        {
            bool[] used = new bool[OppHandKnown.Count];

            foreach (SCard attackCard in move)
            {
                bool found = false;
                for (int i = 0; i < OppHandKnown.Count; i++)
                {
                    if (!used[i] && SCard.CanBeat(attackCard, OppHandKnown[i], TrumpSuit))
                    {
                        used[i] = true;
                        found = true;
                        break;
                    }
                }

                if (!found) return Throw_SuccessFailPenalty;
            }

            double oppPower = 0;
            for (int i = 0; i < OppHandKnown.Count; i++)
            {
                if (used[i]) continue;
                oppPower += OppHandKnown[i].Rank;
                if (OppHandKnown[i].Suit == TrumpSuit) oppPower += 14;
            }

            return -oppPower;
        }

        // Контроль оптимальности защиты
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

                if (def.Suit == TrumpSuit && down.Suit != TrumpSuit)
                    difInRank += def.Rank + (14 - down.Rank);
                else
                    difInRank += def.Rank - down.Rank;
            }

            return DefOptimalBase - difInRank;
        }

        // Бафф за защиту картами, одинакового ранга
        private int GetSameRankDefBuff(List<SCard> move)
        {
            HashSet<int> uniqueRanks = new HashSet<int>();
            foreach (SCard card in move) uniqueRanks.Add(card.Rank);

            if (uniqueRanks.Count == 1) return SameRankDefBuff;
            if (uniqueRanks.Count == 2) return 0;
            return DiffRankDefPenalty;
        }

        // Штраф за атаку козырями
        private int GetAttackTrumpPenalty(List<SCard> cards)
        {
            int penalty = 0;
            foreach (SCard card in cards)
                if (card.Suit == TrumpSuit)
                    penalty += card.Rank;
            return -penalty;
        }

        // Штраф за подкидывание козырями (пртивник отбился) 
        private int GetThrowTrumpPenalty(List<SCard> cards)
        {
            int penalty = 0;
            foreach (SCard card in cards)
                if (card.Suit == TrumpSuit)
                    penalty += card.Rank;
            return -penalty;
        }

        // Штраф за подкидывание козырями (пртивник взял) 
        private int GetGiveTrumpPenalty(List<SCard> cards)
        {
            int penalty = 0;
            foreach (SCard card in cards)
                if (card.Suit == TrumpSuit)
                    penalty += card.Rank;
            return -penalty;
        }

        // Штраф за защиту козырями
        private int GetDefTrumpPenalty(List<SCard> cards)
        {
            int penalty = 0;
            foreach (SCard card in cards)
                if (card.Suit == TrumpSuit)
                    penalty += card.Rank;
            return -penalty;
        }

        // Штраф за атаку последней картой из масти
        private int GetAttackLastCardPenalty(List<SCard> cards)
        {
            int penalty = 0;
            foreach (SCard card in cards)
            {
                if (card.Rank <= Attack_LastCardRankThreshold) continue;

                int suitCount = 0;
                foreach (SCard handCard in Hand)
                    if (handCard.Suit == card.Suit)
                        suitCount++;

                if (suitCount == 1)
                    penalty += Attack_LastCardPenaltyValue;
            }
            return -penalty;
        }

        // Штраф за подброс последней карты из масти (противник отбился)
        private int GetThrowLastCardPenalty(List<SCard> cards)
        {
            int penalty = 0;
            foreach (SCard card in cards)
            {
                if (card.Rank <= Throw_LastCardRankThreshold) continue;

                int suitCount = 0;
                foreach (SCard handCard in Hand)
                    if (handCard.Suit == card.Suit)
                        suitCount++;

                if (suitCount == 1)
                    penalty += Throw_LastCardPenaltyValue;
            }
            return -penalty;
        }

        // Штраф за подброс последней карты из масти (противник взял)
        private int GetGiveLastCardPenalty(List<SCard> cards)
        {
            int penalty = 0;
            foreach (SCard card in cards)
            {
                if (card.Rank <= Give_LastCardRankThreshold) continue;

                int suitCount = 0;
                foreach (SCard handCard in Hand)
                    if (handCard.Suit == card.Suit)
                        suitCount++;

                if (suitCount == 1)
                    penalty += Give_LastCardPenaltyValue;
            }
            return -penalty;
        }

        // Штраф за попытку защитится, если противник отдаёт козыри
        private int GetEnemyTrumpPenalty(List<SCardPair> table)
        {
            int penalty = 0;
            foreach (SCardPair pair in table)
                if (pair.Down.Suit == TrumpSuit)
                    penalty += EnemyTrumpPenValue;
            return -penalty;
        }

        // Штраф за разрушение пар и троек
        private double GetLostPairPenalty(List<SCard> move)
        {
            int count = -move.Count;
            foreach (SCard defCard in move)
                foreach (SCard handCard in Hand)
                    if (defCard.Rank == handCard.Rank)
                        count++;
            return count > 0 ? LostPairPenMult * count : 0;
        }
        #endregion

        #region MainFunc

        private string name = "ЕКБ 4.0";

        // Возращает имя бота
        public string GetName()
        {
            return name;
        }

        // Возращает размер руки
        public int GetCount()
        {
            return Hand.Count;
        }

        // Установка козыря
        public void SetTrump(SCard newTrump)
        {
            Initialize(newTrump);
        }

        // Атака
        public List<SCard> LayCards()
        {
            RemoveCardsFromList(Hand, RemainingDeck);
            IsBotAttack = true;



            List<List<SCard>> allMoves = GetAllAttackMoves();
            List<SCard> best = ChooseBestMove(allMoves, AttackMoveScore);

            foreach (SCard card in best)
                Hand.Remove(card);

            return best;
        }

        // Подброс
        public bool AddCards(List<SCardPair> table, bool opponentDefenced)
        {
            int limit = Math.Min(MGameRules.TotalCards - table.Count,
                                 OppCardCount - table.Count);
            if (limit <= 0) return false;

            List<List<SCard>> moves = GetAllThrowMoves(table, limit);
            if (moves.Count == 0) return false;




            // --- Эвристика для остальных случаев ---
            List<SCard> best;
            bool willThrow;

            if (opponentDefenced)
                willThrow = ChooseThrowMove(moves, ThrowCardScore, out best);
            else
                willThrow = ChooseThrowMove(moves, GiveCardScore, out best);

            if (!willThrow) return false;

            foreach (SCard card in best)
            {
                table.Add(new SCardPair(card));
                Hand.Remove(card);
            }

            return true;
        }

        // Защита
        public bool Defend(List<SCardPair> table)
        {
            List<SCard> attackCards = new List<SCard>();
            for (int i = 0; i < table.Count; i++)
                attackCards.Add(table[i].Down);

            IsBotAttack = false;

            bool canDefend = GetAllDefenceMoves(table, out List<List<SCard>> moves);
            if (!canDefend) return false;



            bool willDefend2 = ChooseDefMove(moves, table, out List<SCard> best2);
            if (!willDefend2) return false;



            int j = 0;
            for (int i = 0; i < table.Count; i++)
            {
                if (!table[i].Beaten)
                {
                    var pair = table[i];
                    pair.SetUp(best2[j], TrumpSuit);
                    table[i] = pair;
                    Hand.Remove(best2[j]);
                    j++;
                }
            }

            return true;
        }

        // Выбор хода Атаки
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

        // Выбор подброса
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
                    for (int j = 0; j < Hand.Count; j++)
                    {
                        if (Hand[j].Rank == move[i].Rank &&
                            Hand[j].Suit == move[i].Suit)
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
            if (best.Count <= 0) return false;
            return bestScore > ThrowDecisionBoard;
        }

        // Выбор защитного хода
        private bool ChooseDefMove(List<List<SCard>> moves, List<SCardPair> table, out List<SCard> best)
        {
            best = new List<SCard>();
            double bestScore = 0;

            foreach (List<SCard> move in moves)
            {
                double score = DefMoveScore(move, table);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = move;
                }
            }
            if (best.Count <= 0) return false;
            return bestScore > DefDecisionBoard;
        }

        #endregion

    }
}
