using System;
using System.Collections.Generic;

namespace CardFool
{
    public class MPlayer2
    {
        private string Name = "Second";
        private List<SCard> hand = new List<SCard>();       // карты на руке
        private Suits trump_suit;
        // Возвращает имя игрока
        public string GetName()
        {
            return Name;
        }
        //Возвращает количество карт на руке
        public int GetCount()
        {
            return hand.Count;
        }
        //Добавление карты в руку, во время добора из колоды, или взятия карт
        public void AddToHand(SCard card)
        {
            hand.Add(card);
            hand = SortCard(hand, trump_suit);

        }

        //Начальная атака
        public List<SCard> LayCards()
        {
            SCard card = hand[0];
            hand.RemoveAt(0);
            return new List<SCard> { card };
        }

        //Защита от карт
        // На вход подается набор карт на столе, часть из них могут быть уже покрыты
        //Защита от карт
        public bool Defend(List<SCardPair> table)
        {
            bool canDef = true;

            for (int j = 0; j < table.Count; j++)
            {
                if (!table[j].Beaten) // Если карта еще не отбита
                {
                    bool defended = false;

                    for (int i = 0; i < hand.Count; i++)
                    {
                        // Проверяем, может ли карта в руке отбить карту на столе
                        if (SCard.CanBeat(table[j].Down, hand[i], trump_suit))
                        {
                            // Если может, то отбиваем и удаляем карту из руки
                            var a = table[j];
                            a.SetUp(hand[i], trump_suit);
                            table[j] = a;
                            hand.RemoveAt(i);
                            defended = true;
                            break;
                        }
                    }

                    // Если не смогли найти карту для защиты
                    if (!defended)
                    {
                        canDef = false;
                        break;
                    }
                }
            }
            if (canDef)
            {
                hand = SortCard(hand, trump_suit);
            }
            return canDef;
        }

        //Добавление карт
        //На вход подается набор карт на столе, а также отбился ли оппонент
        public bool AddCards(List<SCardPair> table, bool OpponentDefenced)
        {
            return false;
        }
        //Вызывается после основной битвы, когда известно отбился ли защищавшийся
        //На вход подается набор карт на столе, а также была ли успешной защита
        public void OnEndRound(List<SCardPair> table, bool IsDefenceSuccesful)
        {

        }
        //Установка козыря, на вход подаётся козырь, вызывается перед первой раздачей карт
        public void SetTrump(SCard NewTrump)
        {
            trump_suit = NewTrump.Suit;
        }

        public List<SCard> SortCard(List<SCard> cards, Suits trumpSuit)
        {
            // Создаём копию списка, чтобы не менять оригинал
            List<SCard> result = new List<SCard>();
            for (int i = 0; i < cards.Count; i++)
            {
                result.Add(cards[i]);
            }

            // Сортировка методом вставки (insertion sort)
            int n = result.Count;
            for (int i = 1; i < n; i++)
            {
                SCard key = result[i];
                int j = i - 1;

                // Сдвигаем элементы, пока не найдём правильное место для key
                while (j >= 0 && CompareCards(result[j], key, trumpSuit) > 0)
                {
                    result[j + 1] = result[j];
                    j--;
                }
                result[j + 1] = key;
            }

            return result;
        }

        // Вспомогательная функция сравнения двух карт
        // Возвращает:
        //   > 0 — a должна идти ПОЗЖЕ b (a "тяжелее")
        //   < 0 — a должна идти РАНЬШЕ b (a "легче")
        //   = 0 — равны (по рангу и типу)
        private int CompareCards(SCard a, SCard b, Suits trumpSuit)
        {
            // 1. Сначала по типу: некозырь (0) < козырь (1)
            int aType = 0;
            if (a.Suit == trumpSuit)
            {
                aType = 1;
            }

            int bType = 0;
            if (b.Suit == trumpSuit)
            {
                bType = 1;
            }

            if (aType != bType)
            {
                return aType - bType; // 0 - 1 = -1 → a раньше b (некозырь перед козырём)
            }

            // 2. Если одинаковый тип — по рангу (меньший ранг — раньше)
            return a.Rank - b.Rank;
        }

    }
}





