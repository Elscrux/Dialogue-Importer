using System;
using System.Text.RegularExpressions;
using Word = Microsoft.Office.Interop.Word;
using Excel = Microsoft.Office.Interop.Excel;
using UI = System.Windows.Forms;
using System.Collections.Generic;

namespace DialogueParser {
    public partial class Form1 : UI.Form {
        const string BRANCH_NAME = "BRANCH";
        const string GREETING_NAME = "GREETING";
        const string FAREWELL_NAME = "FAREWELL";
        const string IDLE_NAME = "IDLE";
        const string SCENE_NAME = "SCENE";
        const string QUEST_SCENE_NAME = "QUESTSCENE";
        const string FIRST_INDEX = "1.";
        static Regex branchRegex = new Regex(@"[0-9]\.");
        static Regex sceneDialogueRegex = new Regex(@"[*:]\.");
        static Regex illegalFileNameRegex = new Regex(@"[:*?""<>|\[\]]");
        static Regex sceneLineRegex = new Regex(@"([\S\s]*): ([\S\s]+)");

        static Form1 form;
        static string docName;
        static Word.Application word;
        static Word.Document doc;
        static Excel.Application excel;
        static Excel.Workbook workbook;
        static Excel.Worksheet worksheet;

        public Form1() {
            InitializeComponent();
            initDropDownItems(Enum.GetNames(typeof(EDialogueMode)));
        }

        private void btnParseDocument_Click(object sender, EventArgs e) {
            form = (Form1) ((UI.Button) sender).FindForm();

            if (!openFile()) return;

            switch (comboBox1.SelectedIndex) {
                case 0:
                    processDialogue();
                    break;
                case 1:
                    processOneLiners(GREETING_NAME);
                    break;
                case 2:
                    processOneLiners(FAREWELL_NAME);
                    break;
                case 3:
                    processOneLiners(IDLE_NAME);
                    break;
                case 4:
                    processScenes(SCENE_NAME);
                    break;
                case 5:
                    processScenes(QUEST_SCENE_NAME);
                    break;
                default:
                    break;
            }

            saveFile();
        }

        public enum EDialogueMode {
            Dialogue = 0,
            Greetings = 1,
            Farewells = 2,
            Idles = 3,
            Scenes = 4,
            QuestScenes = 5
        }

        static bool openFile() {
            //Init dialogs
            UI.OpenFileDialog openFileDialog = new UI.OpenFileDialog();
            openFileDialog.DefaultExt = "*docx";
            openFileDialog.Filter = "Word documents (*.docx)|*.docx| Rich text files (*.rtf)|*.rtf| Open document text (*.odt)|*.odt";

            if (openFileDialog.ShowDialog() == UI.DialogResult.OK) {
                //Word file
                docName = openFileDialog.SafeFileName;
                word = new Word.Application() { Visible = false };
                doc = word.Documents.Open(openFileDialog.FileName, ReadOnly: true);

                //Excel file
                excel = new Excel.Application() { Visible = false };
                workbook = excel.Workbooks.Add();
                worksheet = (Excel.Worksheet) workbook.ActiveSheet;

                return true;
            }

            return false;
        }

        static void saveFile() {
            UI.SaveFileDialog saveFileDialog = new UI.SaveFileDialog();
            saveFileDialog.Filter = "csv files (*.csv)|*.csv";

            //Save csv file
            saveFileDialog.FileName = System.IO.Path.GetFileNameWithoutExtension(docName);
            if (saveFileDialog.ShowDialog() == UI.DialogResult.OK) {
                saveFileDialog.FileName = illegalFileNameRegex.Replace(saveFileDialog.FileName, "");
                workbook.SaveAs(saveFileDialog.FileName, Excel.XlFileFormat.xlCSV, Local: true);
            }

            //UI cleanup
            form.setLabel("");
            form.clearProgressBar();

            //Document cleanup
            workbook.Close();
            doc.Close();
            word.Quit();
        }

        static void processDialogue() {
            //UI init
            form.setProgressBarMax(doc.ListParagraphs.Count);

            var row = 1;
            var column = 0;
            for (int i = 1; i <= doc.ListParagraphs.Count; i++) {
                var paragraph = doc.ListParagraphs[i];
                if (paragraph.Range.Text == null) return;

                var text = formatText(paragraph.Range.Text);
                var listIndex = paragraph.Range.ListFormat.ListString;

                //UI handling
                form.setLabel(text);
                form.incrementProgressBar();
                /*
                //Text is surrounded by [ ] or ( )
                if (((text.StartsWith("[") && text.EndsWith("]")) || (text.StartsWith("(") && text.EndsWith(")"))) && !listIndex.Equals(FIRST_INDEX))
                    continue;*/
                var branchMatch = branchRegex.Match(listIndex);

                //New branch
                if (listIndex.Equals(FIRST_INDEX) || (branchMatch.Success && branchMatch.Length == listIndex.Length && isTopic(paragraph.Range))) {
                    row++;
                    column = 1;
                    worksheet.Cells[row, column] = BRANCH_NAME;
                }

                //New topic
                if (isTopic(paragraph.Range)) {
                    row++;
                    column = 1;
                    worksheet.Cells[row, column] = text;
                    worksheet.Cells[row, 2] = getLinks(doc, row, i);
                } else {
                    //New response
                    String value = worksheet.Cells[row, 1].Value;
                    if (value != null && value.Equals(BRANCH_NAME)) {
                        row++;
                        worksheet.Cells[row, 2] = getLinks(doc, row, i);
                    }
                    if (column < 3) {
                        column = 3;
                    } else {
                        column++;
                    }
                }

                //Write cell
                worksheet.Cells[row, column] = text;
            }
        }

        static void processOneLiners(string name) {
            //UI init
            form.setProgressBarMax(doc.ListParagraphs.Count);

            var row = 1;
            var column = 1;
            for (int i = 1; i <= doc.ListParagraphs.Count; i++) {
                var paragraph = doc.ListParagraphs[i];
                if (paragraph.Range.Text == null)
                    return;

                var text = formatText(paragraph.Range.Text);
                var listIndex = paragraph.Range.ListFormat.ListString;

                //UI handling
                form.setLabel(text);
                form.incrementProgressBar();



                //New greeting
                if (listIndex.Equals(FIRST_INDEX)) {
                    row++;
                    worksheet.Cells[row, column] = name;
                }

                //Write cell
                row++;
                worksheet.Cells[row, column] = text;
            }
        }

        static void processScenes(string type) {
            //UI init
            form.setProgressBarMax(doc.ListParagraphs.Count);

            List<string> speakers = new List<string>();
            string lastSpeaker = "";
            int currentSceneRow = 0;

            var row = 1;
            var column = 0;
            for (int i = 1; i <= doc.ListParagraphs.Count; i++) {
                var paragraph = doc.ListParagraphs[i];
                if (paragraph.Range.Text == null) return;

                var text = formatText(paragraph.Range.Text);
                var listIndex = paragraph.Range.ListFormat.ListString;

                //UI handling
                form.setLabel(text);
                form.incrementProgressBar();

                //New scene
                if (listIndex.Equals(FIRST_INDEX)) {
                    row++;
                    column = 1;
                    currentSceneRow = row;
                    speakers.Clear();
                    lastSpeaker = "";
                    worksheet.Cells[row, column] = type;
                }

                //Match scene line
                Match sceneLineMatch = sceneLineRegex.Match(text);
                if (!sceneLineMatch.Success) continue;

                //Add speaker
                string speaker = sceneLineMatch.Groups[1].Value;
                string line = sceneLineMatch.Groups[2].Value;
                if (!speakers.Contains(speaker)) {
                    speakers.Add(speaker);

                    worksheet.Cells[currentSceneRow, speakers.Count * 2] = speaker;
                    worksheet.Cells[currentSceneRow, (speakers.Count * 2) + 1] = line;
                }

                //Write line
                if (speaker.Equals(lastSpeaker)) {
                    column++;
                    worksheet.Cells[row, column] = line;
                } else {
                    row++;
                    column = 2;
                    lastSpeaker = speaker;
                    worksheet.Cells[row, 1] = speaker;
                    worksheet.Cells[row, column] = line;
                }
            }
        }

        static bool isTopic(Word.Range input) {
            Console.WriteLine(input.Text + input.Bold + " " + (input.Bold == -1));
            return input.Bold == -1;
        }

        static string getLinks(Word.Document doc, int currentRow, int listParagraph) {
            String output = "";
            var index = listParagraph + 1;
            var rowIndex = currentRow - 1;
            var range = doc.ListParagraphs[listParagraph].Range;
            var listIndex = getLastParagraphInListIndex(doc, listParagraph).Range.ListFormat.ListString;
            var lastIndex = 0;

            //(Non) Topic regex settings
            Regex regex;
            if (isTopic(range)) {
                regex = new Regex("^[0-9]+\\.[0-9]+\\.$");

            } else {
                regex = new Regex("^[0-9]+\\.$");
            }

            //Iterate paragraphs
            while (index < doc.ListParagraphs.Count && doc.ListParagraphs[index].Range.Text != null) {
                var nextListIndex = doc.ListParagraphs[index].Range.ListFormat.ListString;
                if (!(nextListIndex.Equals(FIRST_INDEX) || nextListIndex.Length < listIndex.Length || (nextListIndex.Length <= listIndex.Length && isTopic(doc.ListParagraphs[index].Range)))) {
                    var text = doc.ListParagraphs[index].Range.Text.Replace("\r", "");

                    //Text is surrounded by [ ] or ( )
                    if (((text.StartsWith("[") && text.EndsWith("]")) || (text.StartsWith("(") && text.EndsWith(")"))) && !nextListIndex.Equals(FIRST_INDEX)) {
                        index++;
                        continue;
                    }

                    //Increment row index
                    if (isTopic(doc.ListParagraphs[index].Range)) {
                        rowIndex++;
                    }

                    //Test for matching number
                    if (nextListIndex.StartsWith(listIndex) && regex.IsMatch(nextListIndex.Substring(listIndex.Length)) && rowIndex != lastIndex) {
                        if (!(output.Equals("") || output.EndsWith(","))) {
                            output += ", ";
                        }
                        output += rowIndex;
                        lastIndex = rowIndex;
                    }
                    index++;
                } else {
                    break;
                }
            }
            return output;
        }

        static Word.Paragraph getLastParagraphInListIndex(Word.Document doc, int index) {
            Word.Paragraph paragraph = doc.ListParagraphs[index];
            if (index < doc.ListParagraphs.Count) {
                var initialListIndex = doc.ListParagraphs[index].Range.ListFormat.ListString;

                //Iterate paragraphs
                while (index < doc.ListParagraphs.Count) {
                    var listIndex = doc.ListParagraphs[index].Range.ListFormat.ListString;

                    //Check for some paragraph length
                    if (listIndex.Length == initialListIndex.Length) {
                        paragraph = doc.ListParagraphs[index];
                    } else {
                        break;
                    }
                    index++;
                }
            }
            return paragraph;
        }

        static string formatText(string text) {
            return text.Replace("\r", "").Replace("’", "'").Replace("`", "'").Replace("”", "\"").Replace("…", "...").Trim();
        }

        public void comboBox1_SelectedIndexChanged(object sender, EventArgs e) {

        }
    }
}
