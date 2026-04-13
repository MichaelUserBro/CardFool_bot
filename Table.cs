using System.Collections.Generic;
using System.Linq;
using System;

namespace CardFool
{
    internal class Program
    {
        static int Main()
        {
            MTable.WriteToConsole = false;

            int totalGames = 10000;

            // --- Основные счётчики ---
            int p1Wins = 0, p2Wins = 0, draws = 0;

            // --- Очки (карты у проигравшего) ---
            int p1TotalScore = 0; // очки P1 (карты у P2 когда P1 выиграл)
            int p2TotalScore = 0; // очки P2 (карты у P1 когда P2 выиграл)
            int p1TotalLossCards = 0; // карты у P1 когда он проиграл
            int p2TotalLossCards = 0; // карты у P2 когда он проиграл

            // --- Распределение карт у проигравшего ---
            int[] p1LossDistribution = new int[25]; // [i] = сколько раз P1 проиграл с i картами
            int[] p2LossDistribution = new int[25]; // [i] = сколько раз P2 проиграл с i картами

            // --- Винрейт по первому ходу ---
            int p1AttackFirst = 0, p1WinAttackFirst = 0;
            int p1DefendFirst = 0, p1WinDefendFirst = 0;

            // --- Длина игры ---
            int totalRounds = 0;
            int minRounds = int.MaxValue, maxRounds = int.MinValue;

            Console.WriteLine($"Запуск {totalGames} игр...");

            for (int i = 0; i < totalGames; i++)
            {
                // Чередуем кто ходит первым
                bool p1Attacks = (i % 2 == 0);

                MTable table = new MTable(new MPlayer1(), new MPlayer2(), p1Attacks);
                table.Initialize();

                // Считаем раунды
                int rounds = 0;
                while (true)
                {
                    table.PlayPreRound();
                    rounds++;

                    EndRound roundResult;
                    do
                    {
                        roundResult = table.PlayAttack(table.PlayDefence());
                    } while (roundResult == EndRound.Continue);

                    var gameResult = table.PlayPostRound(roundResult);

                    if (gameResult != EndGame.Continue)
                    {
                        int p1Cards = table.Player1.GetCount();
                        int p2Cards = table.Player2.GetCount();

                        // --- Записываем результат ---
                        if (gameResult == EndGame.First)
                        {
                            p1Wins++;
                            p1TotalScore += p2Cards; // очки = карты проигравшего
                            p2TotalLossCards += p2Cards;
                            if (p2Cards < p2LossDistribution.Length)
                                p2LossDistribution[p2Cards]++;
                        }
                        else if (gameResult == EndGame.Second)
                        {
                            p2Wins++;
                            p2TotalScore += p1Cards;
                            p1TotalLossCards += p1Cards;
                            if (p1Cards < p1LossDistribution.Length)
                                p1LossDistribution[p1Cards]++;
                        }
                        else
                        {
                            draws++;
                        }

                        // --- По первому ходу ---
                        if (p1Attacks)
                        {
                            p1AttackFirst++;
                            if (gameResult == EndGame.First) p1WinAttackFirst++;
                        }
                        else
                        {
                            p1DefendFirst++;
                            if (gameResult == EndGame.First) p1WinDefendFirst++;
                        }

                        // --- Раунды ---
                        totalRounds += rounds;
                        if (rounds < minRounds) minRounds = rounds;
                        if (rounds > maxRounds) maxRounds = rounds;

                        break;
                    }
                }

                // Прогресс каждые 10%
                if ((i + 1) % (totalGames / 10) == 0)
                {
                    Console.WriteLine($"  {(i + 1) * 100 / totalGames}% ({i + 1}/{totalGames})");
                }
            }

            // =============================================
            //  ВЫВОД РЕЗУЛЬТАТОВ
            // =============================================
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("  РЕЗУЛЬТАТЫ ТЕСТИРОВАНИЯ");
            Console.WriteLine("========================================");
            Console.WriteLine();

            // --- Общий винрейт ---
            Console.WriteLine($"Всего игр: {totalGames}");
            Console.WriteLine($"P1 ({new MPlayer1().GetName()}) побед: {p1Wins}");
            Console.WriteLine($"P2 ({new MPlayer2().GetName()}) побед: {p2Wins}");
            Console.WriteLine($"Ничьих: {draws}");
            double winrate = (double)p1Wins / (p1Wins + p2Wins) * 100;
            Console.WriteLine($"Винрейт P1: {winrate:F2}%");
            Console.WriteLine();

            // --- Турнирные очки ---
            Console.WriteLine("--- ТУРНИРНЫЕ ОЧКИ ---");
            Console.WriteLine($"P1 набрал очков (карты проигравшего P2): {p1TotalScore}");
            Console.WriteLine($"P2 набрал очков (карты проигравшего P1): {p2TotalScore}");
            Console.WriteLine($"Разница очков (P1-P2): {p1TotalScore - p2TotalScore}");
            if (p1Wins > 0)
                Console.WriteLine($"Среднее карт у P2 при проигрыше: {(double)p2TotalLossCards / p1Wins:F2}");
            if (p2Wins > 0)
                Console.WriteLine($"Среднее карт у P1 при проигрыше: {(double)p1TotalLossCards / p2Wins:F2}");
            Console.WriteLine();

            // --- Винрейт по первому ходу ---
            Console.WriteLine("--- ВИНРЕЙТ ПО ПЕРВОМУ ХОДУ ---");
            if (p1AttackFirst > 0)
                Console.WriteLine($"P1 атакует первым: {(double)p1WinAttackFirst / p1AttackFirst * 100:F2}% ({p1WinAttackFirst}/{p1AttackFirst})");
            if (p1DefendFirst > 0)
                Console.WriteLine($"P1 защищается первым: {(double)p1WinDefendFirst / p1DefendFirst * 100:F2}% ({p1WinDefendFirst}/{p1DefendFirst})");
            Console.WriteLine();

            // --- Длина игры ---
            Console.WriteLine("--- ДЛИНА ИГРЫ ---");
            Console.WriteLine($"Средняя: {(double)totalRounds / totalGames:F1} раундов");
            Console.WriteLine($"Мин: {minRounds}, Макс: {maxRounds}");
            Console.WriteLine();

            // --- Распределение карт у проигравшего ---
            Console.WriteLine("--- РАСПРЕДЕЛЕНИЕ КАРТ У P2 ПРИ ПРОИГРЫШЕ ---");
            for (int i = 1; i < p2LossDistribution.Length; i++)
            {
                if (p2LossDistribution[i] > 0)
                {
                    string bar = new string('#', Math.Max(1, p2LossDistribution[i] * 50 / Math.Max(1, p1Wins)));
                    Console.WriteLine($"  {i,2} карт: {p2LossDistribution[i],5} ({(double)p2LossDistribution[i] / p1Wins * 100:F1}%) {bar}");
                }
            }
            Console.WriteLine();

            Console.WriteLine("--- РАСПРЕДЕЛЕНИЕ КАРТ У P1 ПРИ ПРОИГРЫШЕ ---");
            for (int i = 1; i < p1LossDistribution.Length; i++)
            {
                if (p1LossDistribution[i] > 0)
                {
                    string bar = new string('#', Math.Max(1, p1LossDistribution[i] * 50 / Math.Max(1, p2Wins)));
                    Console.WriteLine($"  {i,2} карт: {p1LossDistribution[i],5} ({(double)p1LossDistribution[i] / p2Wins * 100:F1}%) {bar}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("========================================");

            return 0;
        }
    };

    public class MTable
    {
        int RoundNum = 0;
        public static bool WriteToConsole = true;


        // Колода карт в прикупе
        public List<SCard> deck = new List<SCard>();

        public MPlayer1 Player1;        // игрок 1
        public MPlayer2 Player2;        // игрок 2


        public SCard Trump;             // козырь
        public List<SCardPair> Table = [];   // карты на столе
        public int DumpCount = 0;
        /// <summary>
        /// Атакует ли первый игрок <br></br>
        /// Конструкция вида  IsFirstAttacking ? Player1 : Player2 получает атакующего игрока <br></br>
        /// Конструкция вида !IsFirstAttacking ? Player1 : Player2 получает защищающегося игрока
        /// </summary>
        public bool IsFirstAttacking = true;
        public MTable(MPlayer1 NewPlayer1, MPlayer2 NewPlayer2,
            bool isFirstPlayerAttacking = true)
        {
            IsFirstAttacking = isFirstPlayerAttacking;
            Player1 = NewPlayer1;
            Player2 = NewPlayer2;
        }
        public int GetPlayerCardCount(bool FirstPlayer)
        {
            return FirstPlayer ? Player1.GetCount() : Player2.GetCount();
        }
        public void AddCardToPlayer(bool FirstPlayer, SCard Card)
        {
            if (FirstPlayer)
                Player1.AddToHand(Card);
            else
                Player2.AddToHand(Card);
        }
        /// <summary>
        /// Простейшая реализация игры, полностью проводит игру без дополнительных вмешательств
        /// </summary>
        public EndGame PlayGame()
        {

            Initialize();
            while (true)
            {
                PlayPreRound();

                EndRound RoundResult;
                do
                {
                    RoundResult = PlayAttack(PlayDefence());
                }
                while (RoundResult == EndRound.Continue);

                var GameResult = PlayPostRound(RoundResult);

                if (GameResult != EndGame.Continue)
                    return GameResult;

            }
        }



        /// <summary>
        /// Инициализация Колоды 
        /// </summary>
        public virtual void InitDeck()
        {
            List<SCard> temp = MGameRules.GetDeck();

            // формирование прикупа - перемешиваем карты
            for (int c = 0, end = temp.Count; c < end; c++)
            {
                int num = Random.Shared.Next(temp.Count);
                deck.Add(temp[num]);
                temp.RemoveAt(num);
            }
        }
        public void Initialize()
        {
            InitDeck();
            // формирование козыря
            Trump = deck[0];

            Player1.SetTrump(Trump);
            Player2.SetTrump(Trump);

            // раздача карт первому и второму игроку
            AddCards();
        }
        public void PlayPreRound()
        {
            //Обозначаем роли на текущий ход
            if (WriteToConsole)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("Раунд: " + RoundNum++);
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Атакует: " + (IsFirstAttacking ? Player1.GetName() : Player2.GetName()));
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("Защищается: " + (!IsFirstAttacking ? Player1.GetName() : Player2.GetName()));
                Console.ForegroundColor = ConsoleColor.White;
            }

            // Выкладываем все карты на стол атакующего
            Table = SCardPair.CardsToCardPairs(IsFirstAttacking ? Player1.LayCards() : Player2.LayCards());

            DrawTable("Начальная атака:");

        }
        public EndRound PlayDefence()
        {
            //Обрабатываем зашиту
            var Defenced = !IsFirstAttacking ? Player1.Defend(Table) : Player2.Defend(Table);

            DrawTable("Оборона: ");

            //И если не отбился от хотя бы одной карты, то вызываем ошибку, если заявил что отбился
            if (Defenced && Table.Any(x => !x.Beaten))
                throw new Exception();

            return Defenced ? EndRound.Continue : EndRound.Take;
        }
        public EndRound PlayAttack(EndRound DefenceResult)
        {
            //Обрабатываем атаку
            bool IsDefenced = DefenceResult != EndRound.Take;
            bool added = IsFirstAttacking ? Player1.AddCards(Table, IsDefenced) : Player2.AddCards(Table, IsDefenced);

            DrawTable("Атака: ");

            //Атакующий не может докинуть больше 6 карт
            if (Table.Count > MGameRules.TotalCards)
                throw new Exception();

            //Атакующий не может подкинуть карты, которые не может отбить обороняющийся из-за недостатка карт
            //Формально: количество небитых карт не должно превышать количество карт в руке оппонента
            if (Table.Count(x => !x.Beaten) > GetPlayerCardCount(!IsFirstAttacking))
                throw new Exception();

            if (DefenceResult == EndRound.Take)
            {
                // если не отбился, то принимает
                foreach (var pair in Table)
                {
                    AddCardToPlayer(!IsFirstAttacking, pair.Down);
                    if (pair.Beaten)
                        AddCardToPlayer(!IsFirstAttacking, pair.Up);
                }

                return EndRound.Take;
            }

            // если отбился и подкинули, то продолжаем
            if (added)
                return EndRound.Continue;

            // если отбился, но не подкинули, то успешная защита
            return EndRound.Defend;
        }

        public EndGame PlayPostRound(EndRound RoundResult)
        {
            if (WriteToConsole)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("Результат раунда: " + RoundResult.ToString());
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
            }

            //Вызываем ивент конца раунда у игроков
            Player1.OnEndRound(Table.ToList(), RoundResult == EndRound.Defend);
            Player2.OnEndRound(Table.ToList(), RoundResult == EndRound.Defend);

            //Если защита была успешной, то все карты уходят в стопку сброса
            if (RoundResult == EndRound.Defend)
                DumpCount += Table.Count * 2;

            // Добавляем игрокам карты из колоды
            AddCards();


            // Если игрок защитился даём ему ход
            if (RoundResult == EndRound.Defend)
                IsFirstAttacking = !IsFirstAttacking;

            //Собираем информацию о игроках
            int Player1Count = Player1.GetCount();
            int Player2Count = Player2.GetCount();

            //Проверяем выполнение закона сохранения карт 
            if (DumpCount + Player1Count + Player2Count + deck.Count != 36)
                throw new Exception();

            // Если конец игры, то выходим
            if (Player1Count == 0 && Player2Count == 0) return EndGame.Draw;
            if (Player1Count == 0) return EndGame.First;
            if (Player2Count == 0) return EndGame.Second;
            //А если нет то остаёмся
            return EndGame.Continue;
        }

        // Добавляем карты из колоды первому и второму игроку
        private void AddCards()
        {
            // добавляем карты атаковавшему игроку
            while (GetPlayerCardCount(IsFirstAttacking) < MGameRules.TotalCards && deck.Count > 0)
            {
                AddCardToPlayer(IsFirstAttacking, deck.Last());
                deck.RemoveAt(deck.Count - 1);
            }

            // добавляем защищавшемуся игроку
            while (GetPlayerCardCount(!IsFirstAttacking) < MGameRules.TotalCards && deck.Count > 0)
            {
                AddCardToPlayer(!IsFirstAttacking, deck.Last());
                deck.RemoveAt(deck.Count - 1);
            }
        }
        //Выводим карты на столе на консоль при этом сначала выведя Text
        private void DrawTable(string Text)
        {
            if (!WriteToConsole)
                return;
            Console.WriteLine(Text);
            Console.WriteLine(string.Join("   ", Table.ConvertAll(x => x.ToString())));
        }
    }
}