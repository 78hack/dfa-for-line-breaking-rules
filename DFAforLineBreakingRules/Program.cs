using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DFAforLineBreakingRules
{
    class Program
    {
        private static readonly int[] sigma = { 0, 1 };
        private static readonly int[] next = new int[] { 0, 2, 0, 4, 0, -1 };
        private static readonly int[] check = new int[] { sigma[0], sigma[1], sigma[0], sigma[1], sigma[0], sigma[1] };
        /// <summary>
        /// 日本語における行頭禁則文字
        /// </summary>
        private static readonly string prohibitedCharacters = @"ゝゞーァィゥェォッャュョヮヵヶぁぃぅぇぉっゃゅょゎゕゖㇰㇱㇲㇳㇴㇵㇶㇷㇸㇹㇷ゚ㇺㇻㇼㇽㇾㇿ々〻‐゠–〜～?!‼⁇⁈⁉・:;/。.,)]｝、〕〉》」』】〙〗〟’”｠»";

        /// <summary>
        /// エントリポイント
        /// </summary>
        /// <param name="args">args</param>
        static void Main(string[] args)
        {
            Console.WriteLine(">HI!");
            Console.WriteLine(">HOLD ON A MOMENT...");

            var state = 0;
            var characterCount = 0;
            var targetTable = GetDummyTable().AsEnumerable();
            var recordCount = targetTable.Count();
            var prohibitedCharactersArray = prohibitedCharacters.ToCharArray();
            var twoConsecutiveCharRecords = new List<string>();
            var threeConsecutiveCharRecords = new List<string>();

            Console.WriteLine(">I'M READY, PLEASE ENTER KEY!");
            Console.ReadKey();

            var sw = new Stopwatch();
            sw.Start();

            foreach (var targetRecord in targetTable)
            {
                state = 0;
                var isFirstTime = true;

                var targetCharArray = targetRecord.Field<string>("value").ToCharArray();
                foreach (var targetChar in targetCharArray)
                {
                    characterCount++;

                    if (prohibitedCharactersArray.Contains(targetChar))
                    {
                        state = Delta(state, sigma[1]);
                        if (state == 4 && isFirstTime)
                        {
                            twoConsecutiveCharRecords.Add(targetRecord.Field<string>("value"));
                            isFirstTime = !isFirstTime;
                        }
                        else if (state == -1)
                        {
                            twoConsecutiveCharRecords.Remove(targetRecord.Field<string>("value"));
                            threeConsecutiveCharRecords.Add(targetRecord.Field<string>("value"));
                            // 禁則文字が3連続した時点でそのレコードの走査をやめる
                            break;
                        }
                    }
                    else
                    {
                        state = Delta(state, sigma[0]);
                    }
                }
            }

            sw.Stop();
            var ts = sw.Elapsed;

            OutputResultFile(twoConsecutiveCharRecords, nameof(twoConsecutiveCharRecords));
            OutputResultFile(threeConsecutiveCharRecords, nameof(threeConsecutiveCharRecords));

            Console.WriteLine();
            Console.WriteLine("走査レコード総数: {0:#,0}", recordCount);
            Console.WriteLine("走査文字総数: {0:#,0}", characterCount);
            Console.WriteLine();
            Console.WriteLine("禁則文字が2連続するデータ件数: {0:#,0}", twoConsecutiveCharRecords.Count());
            Console.WriteLine("禁則文字が2連続するデータ件数(重複排除): {0:#,0}", twoConsecutiveCharRecords.Distinct().Count());
            Console.WriteLine();
            Console.WriteLine("禁則文字が3連続するデータ件数: {0:#,0}", threeConsecutiveCharRecords.Count());
            Console.WriteLine("禁則文字が3連続するデータ件数(重複排除): {0:#,0}", threeConsecutiveCharRecords.Distinct().Count());
            Console.WriteLine();
            Console.WriteLine($"走査時間: {ts.TotalSeconds}秒");

            Process.Start(Path.GetDirectoryName(Application.ExecutablePath));
        }

        /// <summary>
        /// 状態遷移関数
        /// </summary>
        /// <param name="q">現状態</param>
        /// <param name="c">入力</param>
        /// <returns>遷移先</returns>
        private static int Delta(int q, int c)
        {
            var t = q + c;
            if (check[t] == c) return next[t];
            else return -1;
        }

        /// <summary>
        /// 結果を記載したテキストファイルを出力する
        /// </summary>
        /// <param name="resultRecordList">結果レコードリスト</param>
        /// <param name="fileName">出力するファイル名</param>
        private static void OutputResultFile(List<string> resultRecordList, string fileName)
        {
            using (var outputFile = new StreamWriter($"{fileName}.txt", false, Encoding.GetEncoding("Shift_JIS")))
            {
                foreach (var record in resultRecordList.Distinct())
                {
                    outputFile.WriteLine(record);
                }
            }
        }

        /// <summary>
        /// 動作確認用ダミーテーブルを取得する
        /// </summary>
        /// <returns>動作確認用ダミーテーブル</returns>
        private static DataTable GetDummyTable()
        {
            var dummyTable = new DataTable("dummyTable");
            dummyTable.Columns.Add("key", typeof(int));
            dummyTable.Columns.Add("value", typeof(string));
            dummyTable.Rows.Add(001, "hello world!");
            dummyTable.Rows.Add(002, "hello world!!"); // 2 Consecutive Char Record
            dummyTable.Rows.Add(003, "hello world!!!"); // 3 Consecutive Char Record
            dummyTable.Rows.Add(004, "ァアィイゥウェエォオ");
            dummyTable.Rows.Add(005, "ァァイゥゥゥ"); // 3 Consecutive Char Record
            dummyTable.Rows.Add(006, "ダミー・レコード"); // 2 Consecutive Char Record
            dummyTable.Rows.Add(007, "ダミー・レコード"); // 2 Consecutive Char Record
            dummyTable.Rows.Add(008, "ダミー・レコード"); // 2 Consecutive Char Record
            dummyTable.Rows.Add(009, "five semicolons;;;;;"); // 5 Consecutive Char Record
            dummyTable.Rows.Add(010, "[][][]");
            dummyTable.Rows.Add(011, "[[[]]]"); // 3 Consecutive Char Record
            dummyTable.Rows.Add(012, "{{{}}}");
            dummyTable.Rows.Add(013, "｛｛｛｝｝｝"); // 3 Consecutive Char Record
            dummyTable.Rows.Add(014, ",,,"); // 3 Consecutive Char Record
            dummyTable.Rows.Add(015, ", , ,");
            dummyTable.Rows.Add(016, ", ,, ,"); // 2 Consecutive Char Record
            dummyTable.Rows.Add(017, ".. ... .."); // 3 Consecutive Char Record
            dummyTable.Rows.Add(018, "|ω・)"); // 2 Consecutive Char Record
            dummyTable.Rows.Add(019, "|ω・)"); // 2 Consecutive Char Record
            dummyTable.Rows.Add(020, "🐕🐕🐕");
            dummyTable.Rows.Add(021, "ーーーーー"); // 5 Consecutive Char Record
            dummyTable.Rows.Add(022, "ーーーーー"); // 5 Consecutive Char Record
            dummyTable.Rows.Add(023, "ーーーーー"); // 5 Consecutive Char Record
            dummyTable.Rows.Add(024, "ﾈｺﾈｺヵヮィィ"); // 4 Consecutive Char Record
            dummyTable.Rows.Add(025, "ﾈｺﾈｺヵヮｨｨ"); // 2 Consecutive Char Record
            return dummyTable;
        }
    }
}
