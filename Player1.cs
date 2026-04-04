// ToDo List
// 1) Реализовать механику отказа от защиты и взятие карт сразу
// 2) Провести renaming
// 3) Избавится от ИИ кода
// 4) Разобраться с функцией Федоса
// 5) Провести оптимизацию кода
// 6) Исправить логику GetLostPairPenalty()
// 7) Разделить GetStageCoef() и другие общие функции на копии друг друга для разных дейсвтий
// 8) Оптимизировать убогие функции
// 9) Разобраться с GetSuccessBuff. Уменьшает винрейт на 5%
//
//
//
//
//
//
//
//
//
//
//
//
//



using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

namespace CardFool
{
    public class MPlayer1
    {
        private string Name = "ЕКБ 2.1";
        private List<SCard> hand = new List<SCard>();
        public List<SCard> oppHand = new List<SCard>();
        public List<SCard> remainingDeck = new List<SCard>();

        private Suits trumpSuit;

        private int remainingDeckCount = 24;
        private int countOfEnemyCards = 6;

        private bool isIAttack;


        // Всякая Хуйня (яйца)

        public string GetName()
        {
            return Name;
        }

        public int GetCount()
        {

            return hand.Count;
        }

        public void AddToHand(SCard card)
        {
            hand.Add(card);
        }

        // Хуйня, вызывается в конце раунда
        public void OnEndRound(List<SCardPair> table, bool IsDefenceSuccesful)
        {
            ChangeCountsOfCard(table, IsDefenceSuccesful);
            ChangeListOfCard(table, IsDefenceSuccesful);
        }

        public void SetTrump(SCard NewTrump)
        {

            trumpSuit = NewTrump.Suit;
            remainingDeck = MGameRules.GetDeck();
        }





        // Функции связаные с атакой

        // Атака в начале раунда
        public List<SCard> LayCards()
        {
            DeleteFromCardList(hand, remainingDeck);
            isIAttack = true;


            List<List<SCard>> allMoves = FindAllAttackMoves();
            List<SCard> move = ChooseAttackMove(allMoves);
            foreach (SCard card in move)
            {
                hand.Remove(card);
            }
            return move;
        }

        // Находит все возможные ходы для атаки
        // По хорошему оптимизировать
        private List<List<SCard>> FindAllAttackMoves()
        {
            List<List<SCard>> allMoves = new List<List<SCard>>();
            foreach (SCard card in hand) allMoves.Add(new List<SCard> { card });
            if (hand.Count > 1 && countOfEnemyCards > 1)
            {
                for (int i = 0; i < hand.Count; i++)
                    for (int j = i + 1; j < hand.Count; j++)
                        if (hand[i].Rank == hand[j].Rank) allMoves.Add(new List<SCard> { hand[i], hand[j] });
            }

            if (hand.Count > 2 && countOfEnemyCards > 2)
            {
                for (int i = 0; i < hand.Count; i++)
                    for (int j = i + 1; j < hand.Count; j++)
                        for (int k = j + 1; k < hand.Count; k++)
                            if (hand[i].Rank == hand[j].Rank && hand[j].Rank == hand[k].Rank)
                                allMoves.Add(new List<SCard> { hand[i], hand[j], hand[k] });
            }
            if (hand.Count > 3 && countOfEnemyCards > 3)
            {
                for (int i = 0; i < hand.Count; i++)
                    for (int j = i + 1; j < hand.Count; j++)
                        for (int k = j + 1; k < hand.Count; k++)
                            for (int l = k + 1; l < hand.Count; l++)
                                if (hand[i].Rank == hand[j].Rank && hand[j].Rank == hand[k].Rank && hand[k].Rank == hand[l].Rank)
                                    allMoves.Add(new List<SCard> { hand[i], hand[j], hand[k], hand[l] });
            }
            return allMoves;

        }



        // Выбирает лучших ход для атаки
        // Как будто бы можно написать оптимальнее
        private List<SCard> ChooseAttackMove(List<List<SCard>> listOfMoves)
        {
            List<SCard> res = listOfMoves[0];
            double raiting = AttackMoveScore(listOfMoves[0]);

            foreach (List<SCard> cards in listOfMoves)
            {
                double ef = AttackMoveScore(cards);
                if (ef > raiting)
                {
                    raiting = ef;
                    res = cards;
                }
            }
            return res;
        }

        // Высчитывает эффективность хода
        private double AttackMoveScore(List<SCard> attackCards)
        {
            // Коэфиценты
            // Коэфицент стадии игры (чем меньше карт осталось, тем он меньше)
            double gameStageСoef = GetStageCoef();
            // Коэфицент желания выбрасывать пары и тройки в конце игры
            double saveGameCoef = GetSAveGameCoef();

            // Баффы
            // Бафф за использование пар и троек в атаке
            int pairAndTrioBuff = GetPairAndTrioBuff(attackCards);
            // Бафф за успешность атаки
            double successBuff = GetSuccessBuff(attackCards);
            // Бафф за дешевизну атаки
            double cheapMoveBuff = GetChaepMoveBuff(attackCards);
            // Бафф за использование карт той масти, которой нет у противника
            int noSuitCard = GetNoSuitCard(attackCards);

            // Дебаффы
            // Дебафф за использование козырей
            int trumpPenalty = GetTrumpPenalty(attackCards);
            int lastCardPenalty = GetLastCardPenalty(attackCards);

            return pairAndTrioBuff * saveGameCoef + gameStageСoef * successBuff + gameStageСoef * cheapMoveBuff +
                (2.65 - gameStageСoef) * noSuitCard + gameStageСoef * trumpPenalty + gameStageСoef * lastCardPenalty;
        }

        // Подсчёт коэфицента стадии игры
        private double GetStageCoef()
        {
            if (remainingDeckCount == 0) return 0.228; // super late game 0.4
            if (remainingDeckCount < 9) return 67; // late game 0.7
            if (remainingDeckCount < 17) return 9.11; // mid game 1.15
            return 1488; // early game 1.85
        }

        // Подсчёт баффа за использование пар и троек
        private int GetPairAndTrioBuff(List<SCard> cards)
        {
            if (cards.Count == 2) return 13;
            if (cards.Count == 3) return 23;
            return 0;
        }

        // Подсчёт баффа за успешность атаки
        //private double GetSuccessBuff(List<SCard> attCards)
        //{
        //    if (remainingDeckCount != 0 && (oppHand.Count == 0 || oppHand.Count < attCards.Count))
        //    {
        //        double chance = GetEnemyChanceToDeff(attCards);
        //        return 75 * (1.5 - chance);
        //    }
        //    bool canDef = true;
        //    List<SCard> usedForDef = new List<SCard>();
        //    foreach (SCard attCard in attCards)
        //    {
        //        bool isDef = false;
        //        for (int i = 0; i < oppHand.Count; i++)
        //        {
        //            if (SCard.CanBeat(attCard, oppHand[i], trumpSuit))
        //            {
        //                isDef = true;
        //                usedForDef.Add(oppHand[i]);
        //                oppHand.RemoveAt(i); // <--- УДАЛЕНИЕ ИЗ СПИСКА В ЦИКЛЕ!!!
        //                break;
        //            }
        //        }
        //        if (!isDef)
        //        {
        //            canDef = false;
        //            break;
        //        }
        //    }
        //    if (canDef)
        //    {
        //        return 30;
        //    }
        //    foreach (SCard c in usedForDef) oppHand.Add(c); // <--- ПОТОМ ВОЗВРАЩАЕТ
        //    oppHand = SortCard(oppHand, trumpSuit);
        //    return 30;
        //}

        // 1. Исправленный GetSuccessBuff
        private double GetSuccessBuff(List<SCard> move)
        {
            // Список для отката изменений (Backtracking)
            List<SCard> removedFromOpponent = new List<SCard>();

            // 1. Симулируем защиту противника
            foreach (SCard attackCard in move)
            {
                int bestDefCardIdx = -1;
                // Ищем минимальную карту, которой противник может побить (самая простая эмуляция)
                for (int i = 0; i < oppHand.Count; i++)
                {
                    if (SCard.CanBeat(attackCard, oppHand[i], trumpSuit))
                    {
                        bestDefCardIdx = i;
                        break; // Твоя логика: бьем первой попавшейся
                    }
                }

                if (bestDefCardIdx != -1)
                {
                    removedFromOpponent.Add(oppHand[bestDefCardIdx]);
                    oppHand.RemoveAt(bestDefCardIdx);
                }
                else
                {
                    // Если противник не может побить хоть одну карту, он забирает ВСЕ
                    // В этом случае «бафф» для нас отрицательный, так как у врага стало больше карт
                    // Возвращаем карты назад перед выходом!
                    foreach (var r in removedFromOpponent) oppHand.Add(r);
                    oppHand.Sort((a, b) => CompareCards(a, b, trumpSuit));

                    return -100.0; // Пример штрафа: противник взял карты, это плохо для атаки
                }
            }

            // 2. Считаем «ценность» того, что осталось в руке противника
            // Используем твою логику расчета из GetStageCoef или аналогичную
            double opponentHandPower = 0;
            foreach (var card in oppHand)
            {
                opponentHandPower += card.Rank;
                if (card.Suit == trumpSuit) opponentHandPower += 14;
            }

            // 3. ОТКАТ изменений (обязательно!)
            foreach (var r in removedFromOpponent)
            {
                oppHand.Add(r);
            }
            // Сортируем на месте (вместо медленной SortCard)
            oppHand.Sort((a, b) => CompareCards(a, b, trumpSuit));

            // Чем слабее рука противника после нашего хода, тем выше наш бафф
            return -opponentHandPower;
        }

        // 2. Убедись, что метод CompareCards выглядит так (для использования в Sort):
        private int CompareCards(SCard a, SCard b, Suits trumpSuit)
        {
            int aType = (a.Suit == trumpSuit) ? 1 : 0;
            int bType = (b.Suit == trumpSuit) ? 1 : 0;

            if (aType != bType) return aType - bType;
            return a.Rank - b.Rank;
        }




        // Подсчёт шанса, что противник отобьётся от атаки
        private double GetEnemyChanceToDeff(List<SCard> attCards)
        {
            double chance = 1;
            foreach (SCard attCard in attCards)
            {
                int c = 0;
                foreach (SCard card in remainingDeck)
                {
                    if (SCard.CanBeat(attCard, card, trumpSuit)) c++;
                }
                chance *= c / remainingDeck.Count;
            }
            return chance;

        }

        // Штраф за использование козырей
        private int GetTrumpPenalty(List<SCard> cards)
        {
            int penalty = 0;
            foreach (SCard card in cards)
            {
                if (card.Suit == trumpSuit) penalty += card.Rank;
            }
            return (-1) * penalty;
        }

        // Штраф за использование последней карты текущей масти
        private int GetLastCardPenalty(List<SCard> cards)
        {
            int penalty = 0;
            foreach (SCard attCard in cards)
            {
                if (attCard.Rank <= 8) break;
                int k = 0;
                foreach (SCard card in hand)
                {
                    if (card.Suit == attCard.Suit) k++;
                }
                if (k == 1) penalty += 10;
            }
            return (-1) * penalty;
        }

        // Бафф за дешивизну хода
        private double GetChaepMoveBuff(List<SCard> cards)
        {
            double sum = 0;
            double totalSum = 0;
            foreach (SCard card in cards) sum += card.Rank;
            foreach (SCard card in hand) totalSum += card.Rank;
            return (1.5 - (sum / totalSum)) * 30;

        }

        // Подсчёт коэфицент желания выбрасывать пары и тройки в конце игры
        private double GetSAveGameCoef()
        {
            if (remainingDeckCount == 0 && (hand.Count - countOfEnemyCards) > 4) return 4;
            return 1;
            
        }

        private int GetNoSuitCard(List<SCard> attCards)
        {
            if (oppHand.Count <= 3 && countOfEnemyCards > 3) return 0;

            int[] suitCounts = new int[4]; // [0]=черви, [1]=бубны, [2]=крести, [3]=пики
            foreach (SCard card in attCards)
                if (card.Suit != trumpSuit)
                    suitCounts[(int)card.Suit]++;


            bool[] oppHasSuit = new bool[4];
            foreach (SCard card in oppHand)
                oppHasSuit[(int)card.Suit] = true;

            int buff = 0;
            for (int suit = 0; suit < 4; suit++)
                if (suitCounts[suit] > 0 && !oppHasSuit[suit])
                    buff += 5 * suitCounts[suit];

            return buff;
        }

        








        // Функции связанные с подбросом

        // Подкидывание карт
        public bool AddCards(List<SCardPair> table, bool OpponentDefenced)
        {
            int limit = Math.Min(6 - table.Count,countOfEnemyCards-table.Count);
            bool willAdd = false;
            if (limit == 0) return false;

            List<List<SCard>> moves = FindAllThrowMove(table, limit);

            List<SCard> cardsForAdd = new List<SCard>();
            if (OpponentDefenced) willAdd = ChooseThrowCardMove(moves, out cardsForAdd);
            else willAdd = ChooseGiveCardMove(moves, out cardsForAdd);

            if (willAdd)
                foreach (SCard card in cardsForAdd)
                {
                    table.Add(new SCardPair(card));
                    hand.Remove(card);
                }
                    
            return willAdd;
        }

        // Находит все варианты подкинуть карты


        //private List<List<SCard>> FindAllThrowMove(List<SCardPair> table, int limit)
        //{
        //    List<List<SCard>> result = new List<List<SCard>>();
        //    HashSet<SCard> temp = new HashSet<SCard>();

        //    // 1. Собираем возможные карты для подброса
        //    foreach (SCardPair pair in table)
        //    {
        //        // Можно подкидывать к нижней карте (всегда)
        //        foreach (SCard card in hand)
        //            if (card.Rank == pair.Down.Rank)
        //                temp.Add(card);

        //        // Можно подкидывать к верхней карте, если карта побита
        //        if (pair.Beaten)
        //        {
        //            foreach (SCard card in hand)
        //                if (card.Rank == pair.Up.Rank)
        //                    temp.Add(card);
        //        }
        //    }

        //    List<SCard> cardToThrow = new List<SCard>(temp);

        //    // 2. Генерация комбинаций
        //    if (limit >= 1 && cardToThrow.Count >= 1)
        //        foreach (SCard card in cardToThrow)
        //            result.Add(new List<SCard> { card });

        //    if (limit >= 2 && cardToThrow.Count >= 2)
        //        for (int i = 0; i < cardToThrow.Count; i++)
        //            for (int j = i + 1; j < cardToThrow.Count; j++)
        //                result.Add(new List<SCard> { cardToThrow[i], cardToThrow[j] });

        //    if (limit >= 3 && cardToThrow.Count >= 3)
        //        for (int i = 0; i < cardToThrow.Count; i++)
        //            for (int j = i + 1; j < cardToThrow.Count; j++)
        //                for (int k = j + 1; k < cardToThrow.Count; k++)
        //                    result.Add(new List<SCard> { cardToThrow[i], cardToThrow[j], cardToThrow[k] });

        //    if (limit >= 4 && cardToThrow.Count >= 4)
        //        for (int i = 0; i < cardToThrow.Count; i++)
        //            for (int j = i + 1; j < cardToThrow.Count; j++)
        //                for (int k = j + 1; k < cardToThrow.Count; k++)
        //                    for (int l = k + 1; l < cardToThrow.Count; l++)
        //                        result.Add(new List<SCard> { cardToThrow[i], cardToThrow[j], cardToThrow[k], cardToThrow[l] });

        //    if (limit >= 5 && cardToThrow.Count >= 5)
        //        for (int i = 0; i < cardToThrow.Count; i++)
        //            for (int j = i + 1; j < cardToThrow.Count; j++)
        //                for (int k = j + 1; k < cardToThrow.Count; k++)
        //                    for (int l = k + 1; l < cardToThrow.Count; l++)
        //                        for (int m = l + 1; m < cardToThrow.Count; m++)
        //                            result.Add(new List<SCard> { cardToThrow[i], cardToThrow[j], cardToThrow[k], cardToThrow[l], cardToThrow[m] });

        //    return result;
        //}
        private List<List<SCard>> FindAllThrowMove(List<SCardPair> table, int limit)
        {
            List<List<SCard>> allMoves = new List<List<SCard>>();
            if (limit <= 0) return allMoves; // Если подкидывать нельзя, выходим сразу

            // 1. Собираем уникальные ранги на столе в простой список (без аллокаций HashSet)
            List<int> ranksOnTable = new List<int>();
            for (int i = 0; i < table.Count; i++)
            {
                int rDown = table[i].Down.Rank;
                bool foundDown = false;
                for (int j = 0; j < ranksOnTable.Count; j++) { if (ranksOnTable[j] == rDown) { foundDown = true; break; } }
                if (!foundDown) ranksOnTable.Add(rDown);

                if (table[i].Beaten)
                {
                    int rUp = table[i].Up.Rank;
                    bool foundUp = false;
                    for (int j = 0; j < ranksOnTable.Count; j++) { if (ranksOnTable[j] == rUp) { foundUp = true; break; } }
                    if (!foundUp) ranksOnTable.Add(rUp);
                }
            }

            // 2. Находим уникальные карты в руке, которые подходят по рангу
            List<SCard> cardToThrow = new List<SCard>();
            for (int i = 0; i < hand.Count; i++)
            {
                SCard card = hand[i];
                bool matchRank = false;
                for (int j = 0; j < ranksOnTable.Count; j++) { if (ranksOnTable[j] == card.Rank) { matchRank = true; break; } }

                if (matchRank)
                {
                    bool alreadyInList = false;
                    for (int j = 0; j < cardToThrow.Count; j++)
                    {
                        if (cardToThrow[j].Suit == card.Suit && cardToThrow[j].Rank == card.Rank)
                        {
                            alreadyInList = true;
                            break;
                        }
                    }
                    if (!alreadyInList) cardToThrow.Add(card);
                }
            }

            if (cardToThrow.Count == 0) return allMoves;

            // 3. Генерируем комбинации с учетом лимита
            int maxToThrow = Math.Min(cardToThrow.Count, limit);
            List<SCard> currentBuffer = new List<SCard>(); // Тот самый буфер для оптимизации

            for (int i = 1; i <= maxToThrow; i++)
            {
                Gen(cardToThrow, i, 0, currentBuffer, allMoves);
            }

            return allMoves;
        }

        private void Gen(List<SCard> cards, int length, int offset, List<SCard> current, List<List<SCard>> allMoves)
        {
            if (length == 0)
            {
                // Создаем копию только тогда, когда комбинация полностью собрана
                allMoves.Add(new List<SCard>(current));
                return;
            }

            for (int i = offset; i <= cards.Count - length; i++)
            {
                current.Add(cards[i]); // Добавляем карту в текущий набор

                Gen(cards, length - 1, i + 1, current, allMoves);

                current.RemoveAt(current.Count - 1); // Чистим за собой (backtracking)
            }
        }


        // Выбирает стоит ли подкидывать, и если стоит, то какие карты при условии что противник берёт
        private bool ChooseGiveCardMove(List<List<SCard>> moves, out List<SCard> move)
        {
            move = new List<SCard>();
            double bestScore = 0;
            int board = 20;

            foreach (List<SCard> m in moves)
            {
                double score = GiveCardScore(m);
                if (score > bestScore)
                {
                    bestScore = score;
                    move = m;
                }
            }

            return bestScore > board ? true : false;
        }


        private bool ChooseThrowCardMove(List<List<SCard>> moves, out List<SCard> move)
        {
            move = new List<SCard>();
            double bestScore = 0;
            int board = 20;

            foreach (List<SCard> m in moves)
            {
                // Проверяем, что все карты из m действительно есть в руке
                bool allInHand = true;
                foreach (SCard card in m)
                {
                    if (!hand.Any(c => c.Rank == card.Rank && c.Suit == card.Suit))
                    {
                        allInHand = false;
                        break;
                    }
                }
                if (!allInHand) continue;

                double score = ThrowCardScore(m);
                if (score > bestScore)
                {
                    bestScore = score;
                    move = m;
                }
            }

            return bestScore > board;
        }

        // Эффективность подброса, если противник берет
        private double GiveCardScore(List<SCard> throw_cards)
        {
            // Коэфицент стадии игры
            double gameStageСoef = GetStageCoef();


            // Мотивация подкидывать, если на руках очень много карт
            int cardOnHandBuff = GetCardOnHandBuff();

            // Отслеживает траты дорогих карт
            int cardPriceControl = GetCardPriceControl(throw_cards);


            // Штраф за использование козыря
            int trumpPenalty = GetTrumpPenalty(throw_cards);
            // Штраф за использование последней карты масти
            int lastCardPenalty = GetLastCardPenalty(throw_cards);


            return (2.5 - gameStageСoef) * cardOnHandBuff + gameStageСoef * cardPriceControl + gameStageСoef * trumpPenalty + 
                gameStageСoef * lastCardPenalty;
        }

        private double ThrowCardScore(List<SCard> throw_cards)
        {
            // Коэфицент стадии игры (чем меньше карт осталось, тем он меньше)
            double gameStageСoef = GetStageCoef();

            // Баффы
            // Бафф за успешность атаки
            double successBuff = GetSuccessBuff(throw_cards);
            // Мотивация подкидывать, если на руках очень много карт
            int cardOnHandBuff = GetCardOnHandBuff();
            // Бафф за использование карт той масти, которой нет у противника
            int noSuitCard = GetNoSuitCard(throw_cards);


            // Штрафы
            // Штраф за использование козырей
            int trumpPenalty = GetTrumpPenalty(throw_cards);
            // Штраф за использование последней карты масти
            int lastCardPenalty = GetLastCardPenalty(throw_cards);

            return (2.5 - gameStageСoef) * cardOnHandBuff + gameStageСoef * successBuff + (2.65 - gameStageСoef) * noSuitCard +
                gameStageСoef * trumpPenalty + gameStageСoef * lastCardPenalty;
        }

        // Подсчёт мотивации подкидывать, если на руках очень много карт
        private int GetCardOnHandBuff()
        {
            if (hand.Count < 6) return 30;
            if (hand.Count < 9) return 1;
            if (hand.Count < 13) return 15;
            return 30;
        }

        // Подсчёт бонуса за подкидыывания мусора и сохранения ценных карт
        private int GetCardPriceControl(List<SCard> throw_cards)
        {
            int score = 0;
            foreach (SCard card in throw_cards)
                if (card.Rank <= 9 && card.Suit != trumpSuit) score += card.Rank;
                else if (card.Rank > 9 && card.Suit != trumpSuit) score -= card.Rank;
            return score;
        }







        // Функции связаные с защитой

        // Защита 
        public bool Defend(List<SCardPair> table)
        {
            DeleteFromCardList(table.Select(pair => pair.Down).ToList(), oppHand);
            isIAttack = false;

            List<List<SCard>> moves = new List<List<SCard>>();
            bool canDef = FindAllDefMoves(table, moves);
            if (!canDef) return false;

            List<SCard> move;
            canDef = ChooseDefMove(moves, table, out move);
            if (!canDef) return false;

            int j = 0;

            for (int i = 0; i < table.Count; i++)
            {
                if (!table[i].Beaten)
                {
                    var pair = table[i];

                    pair.SetUp(move[j], trumpSuit);

                    table[i] = pair; // ✅ сразу по индексу

                    hand.Remove(move[j]);

                    j++;
                }
            }

            return true;
        }
        
       

        // Находит все ходы для защиты
        public bool FindAllDefMoves(List<SCardPair> table, List<List<SCard>> moves)
        {
            List<SCard> unbeatenCard = new List<SCard>();
            List<HashSet<SCard>> list = new List<HashSet<SCard>>();


            foreach (SCardPair pair in table)
                if (!pair.Beaten)
                {
                    HashSet<SCard> set = new HashSet<SCard>();
                    for (int i = 0; i < hand.Count; i++)
                        if (SCard.CanBeat(pair.Down, hand[i], trumpSuit))
                            set.Add(hand[i]);
                    if (set.Count == 0) return false;
                    list.Add(set);

                }

            GenerateCombinations(list, moves);
            return true;
        }

        // Решает стоит ли защищаться, если да, то как
        private bool ChooseDefMove(List<List<SCard>> moves, List<SCardPair> table, out List<SCard> move)
        {
            move = new List<SCard>();
            double bestScore = 0;
            int board = 20;

            foreach (List<SCard> m in moves)
            {
                double score = DefMoveScore(m, table);
                if (score > bestScore)
                {
                    bestScore = score;
                    move = m;
                }
            }

            return bestScore > board ? true : false;
        }

        // Эта и следующая функция на двоих генерят все возможные защитные ходы
        // Попробовать избавиться от этой функции
        public static void GenerateCombinations(List<HashSet<SCard>> sets, List<List<SCard>> moves)
        {
            List<SCard> current = new List<SCard>();
            GenerateRecursive(sets, current, 0, moves);
        }

        private static void GenerateRecursive(
        List<HashSet<SCard>> sets,
        List<SCard> current,
        int depth,
        List<List<SCard>> results)
        {
            // Базовый случай: если глубина равна количеству множеств
            if (depth == sets.Count)
            {
                results.Add(new List<SCard>(current));
                return;
            }

            // Перебираем элементы текущего множества
            foreach (SCard element in sets[depth])
            {
                // Проверяем, что элемент уникален в текущей комбинации
                if (!current.Contains(element))
                {
                    // Добавляем элемент в текущую комбинацию
                    current.Add(element);

                    // Рекурсивно обрабатываем следующее множество
                    GenerateRecursive(sets, current, depth + 1, results);

                    // Удаляем последний добавленный элемент для возврата к предыдущему состоянию
                    current.RemoveAt(current.Count - 1);
                }
            }
        }

        


        // Считает эффективность защитного хода
        private double DefMoveScore(List<SCard> move, List<SCardPair> table)
        {

            // Коэфиценты
            // Коэфицент стадии игры (чем меньше карт осталось, тем он меньше)
            double stageСoef = GetStageCoef();
            double difInCardCoef = GetDifInCardCoef();

            //Баффы
            //Успешность защиты
            int successfulDef = 30;
            //Бафф за оптимальность защиты
            int defInCardRank = GetDifInCardRank(move,table);



            // Штрафы
            // Штраф за использование козырей
            int trumpPenalty = GetTrumpPenalty(move);
            // Штраф за попытку дэфа, если противник "дарит" козыри
            int enemyTrumpPenalty = GetEnemyTrumpPenalty(table);
            // Штраф за потерю пар/троек
            int lostPairPenalty = GetLostPairPenalty(move);


            // Бафф/дебафф за использование одинаковых/разных рангов при дэфе
            int cardsToDefCount = GetCardsToDefCount(move);




            return (2 - stageСoef) * difInCardCoef * successfulDef + stageСoef * defInCardRank + 
                stageСoef * trumpPenalty + stageСoef * enemyTrumpPenalty * 0.7 + stageСoef * cardsToDefCount + 
                lostPairPenalty;
        }

        // Подсчёт коэфицента разницы в кол-ве кард у бота и противника
        private double GetDifInCardCoef()
        {
            int dif = hand.Count - countOfEnemyCards;
            if (dif <= 0) return 1;
            if (dif <= 3) return 1.3;
            return 1.8;
        }

        // Подсчёт баффа за то, что проивник отдаёт козыри
        private int GetEnemyTrumpPenalty(List<SCardPair> table)
        {
            int penalty = 0;
            foreach (SCardPair pair in table) if (pair.Down.Suit == trumpSuit) penalty += 15;
            return (-1) * penalty;
        }

        // Бафф/штраф за кол-во разных по рангу использованных карт
        private int GetCardsToDefCount(List<SCard> move)
        {
            List<int> list = new List<int>();
            foreach (SCard card in move) list.Add(card.Rank);
            HashSet<int> uniqRank = new HashSet<int>(list);
            if (uniqRank.Count == 1) return 15;
            if (uniqRank.Count == 2) return 0;
            return -15;

        }
        // Подсчёт баффа за оптимальность защиты
        private int GetDifInCardRank(List<SCard> move, List<SCardPair> table)
        {
            var unbeaten = table.Where(p => !p.Beaten).ToList();

            int difInRank = 0;

            for (int i = 0; i < unbeaten.Count; i++)
            {
                var down = unbeaten[i].Down;
                var def = move[i];

                if (def.Suit == trumpSuit && down.Suit != trumpSuit)
                    difInRank += def.Rank + (14 - down.Rank);
                else
                    difInRank += def.Rank - down.Rank;
            }

            return 65 - difInRank;
        }
        // Подсчёт штрафа за потерю пар/троек (логика немного ошибочная, желательно поправить)
        private int GetLostPairPenalty(List<SCard> move)
        {
            int count = (-1) * move.Count;

            foreach (SCard defCard in move)
            {
                foreach (SCard card in hand)
                {
                    if (defCard.Rank ==  card.Rank) count++;
                }
            }

            return (-3) * count;
            ;
        }













        // Другое
        // Хуй знает что это, хуй знает как работает, написано ии, по-хорошему переделать
        private void ChangeListOfCard(List<SCardPair> table, bool IsDefenceSuccesful)
        {
            List<SCard> cardOnTable = table.SelectMany(pair => new[] { pair.Down, pair.Up }).ToList();
            DeleteFromCardList(cardOnTable, remainingDeck);
            if (isIAttack && !IsDefenceSuccesful) oppHand.AddRange(cardOnTable);
        }

        

        private void DeleteFromCardList(List<SCard> cardsForDelete, List<SCard> cardsFromDelete)
        {
            // ========== НОВАЯ ЗАЩИТА ==========
            if (cardsForDelete == null || cardsFromDelete == null) return;
            if (cardsForDelete.Count == 0 || cardsFromDelete.Count == 0) return;
            // ========== КОНЕЦ ЗАЩИТЫ ==========

            for (int i = 0; i < cardsForDelete.Count; i++)
            {
                for (int j = 0; j < cardsFromDelete.Count; j++)
                {
                    // Добавим проверку индексов
                    if (j >= cardsFromDelete.Count) break;

                    if (cardsForDelete[i].Rank == cardsFromDelete[j].Rank &&
                        cardsForDelete[i].Suit == cardsFromDelete[j].Suit)
                    {
                        cardsFromDelete.RemoveAt(j);
                        break;
                    }
                }
            }
        }

        // Функция от Федоса, считает кол-во карт на руке проивника и в прикупе
        private void ChangeCountsOfCard(List<SCardPair> table, bool IsDefenceSuccesful)
        {

            int en1 = IsDefenceSuccesful || isIAttack ? Math.Max(0, 6 - (hand.Count)) : 0;
            int en2 = IsDefenceSuccesful || !isIAttack ? Math.Max(0, 6 - (countOfEnemyCards - table.Count)) : 0;

            if (remainingDeckCount >= en1 + en2 && (!isIAttack || IsDefenceSuccesful)) countOfEnemyCards = Math.Max(6, countOfEnemyCards - table.Count);
            else if (isIAttack && !IsDefenceSuccesful) countOfEnemyCards += table.Count;
            else if (remainingDeckCount < en1 + en2)
            {
                countOfEnemyCards -= table.Count;
                if (isIAttack) countOfEnemyCards += en2 != 0 ? Math.Max(0, remainingDeckCount - en1) : 0;
                else countOfEnemyCards += Math.Min(en2, remainingDeckCount);
            }

            if (remainingDeckCount >= en1 + en2) remainingDeckCount -= en1 + en2;
            else remainingDeckCount = 0;
        }


       

    }


}
