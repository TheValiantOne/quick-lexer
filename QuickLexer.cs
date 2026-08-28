using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Data.Sqlite;

namespace DataLex
{

    #region QuickLexerClass
    public class QuickLexer
    {
        public DelimiterChars delimiters;
        private bool headerExists;
        private String sourceFileName;

        #region Constructors
        public QuickLexer(String filePath, char columnSeparator, char textIdentifier, char newLineChar)
        {
            this.sourceFileName = filePath;
            this.delimiters = new DelimiterChars(columnSeparator, textIdentifier, newLineChar);
            //this.quickLexDB = new SqliteDatabase("Data Source=:memory:"); 
            
        }
        #endregion Constructors

        #region publicMethods
        public DataTable GetDataTableFromFilePath()
        {
            string newTableName = NormalizeDataTableName(this.sourceFileName);
            Console.WriteLine("Now loading " + newTableName + " . . .");
            var quickLexerLineData = new QuickLexerLineData(new StreamReader(this.sourceFileName), this.delimiters);
            DataTable quickLexerTable = new DataTable(newTableName);
            try
            {
                var saveDB = new SqliteConnection("Data Source ='C:\\temp\\testDatabase.sqlite'");
                var quickLexDB = new SqliteConnection("Data Source ='C:\\temp\\testDatabase.sqlite';Mode=Memory;Cache=Shared");
                quickLexDB.Open();
                saveDB.Open();
                saveDB.BackupDatabase(quickLexDB);
                saveDB.Close();

                while (quickLexerLineData.reader.Peek() > 0)
                {
                    List<String> values = quickLexerLineData.GetNextRowDataFromStreamReader();

                    if (!this.headerExists)
                    {
                        quickLexerTable = CreateQuickLexerTable(values, quickLexerTable);
                        CreateTable(newTableName, values, quickLexDB);
                        this.headerExists = true;
                    }
                    else
                    {
                        quickLexerTable = InsertValuesToQuickLexerTable(values, quickLexerTable);
                        _ = InsertDataToTableAsync(newTableName, values, quickLexDB);
                    }
                }

                //Console.WriteLine("]");
                saveDB.Open();
                quickLexDB.BackupDatabase(saveDB);
                saveDB.Close();
                quickLexDB.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.InnerException);
                Console.WriteLine(e.StackTrace);
            }

            return quickLexerTable;
            
        }
        #endregion publicMethods

        #region PrivateClasses

        #endregion Private Classes

        #region privateMethods
        private string NormalizeDataTableName(string dataTablePath)
        {
            String[] sections = dataTablePath.Split("\\");
            String dataTableName = sections[sections.Length - 1];
            return dataTableName.Remove(dataTableName.Length - 4).Replace(" ", "_").Replace('\t', '_').Replace('\n', '_');
        }

        private void CreateTable(string tableName, List<String> columns, SqliteConnection sqliteConnection)
        {
            string dropTable = "DROP TABLE IF EXISTS [" + tableName + "];";
            var cmd = new SqliteCommand(dropTable, sqliteConnection);
            cmd.ExecuteNonQuery();

            string createTable = "CREATE TABLE IF NOT EXISTS [" + tableName + "] (";

            for (int i = 0; i < columns.Count - 1; i++)
            {
                createTable = createTable + "[" + columns[i].Replace('\t', (Char)32).Replace('\n', (Char)32) + "] TEXT, ";
            }

            var column = columns[columns.Count - 1];

            createTable = createTable + "[" + column.Replace('\t', (Char)32).Replace('\n', (Char)32) + "] TEXT);";

            cmd = new SqliteCommand(createTable, sqliteConnection);
            cmd.ExecuteNonQuery();
        }

        private void InsertDataToTable(string tableName, List<String> columnValues, SqliteConnection sqliteConnection)
        {
            try
            {
                string insertStatement = "INSERT INTO [" + tableName + "] VALUES (";

                for (int i = 0; i < columnValues.Count - 1; i++)
                {
                    insertStatement = insertStatement + "'" + columnValues[i].Replace("'", "''") + "', ";
                }

                var column = columnValues[columnValues.Count - 1];

                insertStatement = insertStatement + "'" + columnValues[columnValues.Count - 1].Replace("'", "''") + "');";

                var cmd = new SqliteCommand(insertStatement, sqliteConnection);
                cmd.ExecuteNonQuery();
            } catch (Exception e)
            {
                Console.WriteLine("Name of Table: " + tableName);
                Console.WriteLine("Count of Parsed Values: " + columnValues.Count);
                Console.WriteLine(e.InnerException);
                Console.WriteLine(e.StackTrace);
            }
        }

        private static async Task InsertDataToTableAsync(string tableName, List<String> columnValues, SqliteConnection sqliteConnection)
        {
            try
            {
                string insertStatement = "INSERT INTO [" + tableName + "] VALUES (";

                for (int i = 0; i < columnValues.Count - 1; i++)
                {
                    insertStatement = insertStatement + "'" + columnValues[i].Replace("'", "''") + "', ";
                }

                var column = columnValues[columnValues.Count - 1];

                insertStatement = insertStatement + "'" + columnValues[columnValues.Count - 1].Replace("'", "''") + "');";

                var cmd = new SqliteCommand(insertStatement, sqliteConnection);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception e) when (columnValues.Count > 0)
            {
                Console.WriteLine("Name of Table: " + tableName);
                Console.WriteLine("Count of Parsed Values: " + columnValues.Count);
                Console.WriteLine(e.InnerException);
                Console.WriteLine(e.StackTrace);
            }
        }

        private DataTable CreateQuickLexerTable(List<String> columns, DataTable quickLexerTable)
        {
            foreach (String column in columns)
            {
                DataColumn dataColumn = new DataColumn();
                dataColumn.DataType = Type.GetType("System.String");
                dataColumn.ColumnName = column.Replace('\t', (Char)32).Replace('\n', (Char)32);
                dataColumn.AutoIncrement = false;
                dataColumn.Caption = column;
                dataColumn.ReadOnly = false;
                dataColumn.Unique = false;

                quickLexerTable.Columns.Add(dataColumn);
            }

            return quickLexerTable;
        }

        private DataTable InsertValuesToQuickLexerTable(List<String> columns, DataTable quickLexerTable)
        {
            DataRow dataRow = quickLexerTable.NewRow();

            for (int i = 0; i < columns.Count; i++)
            {
                try
                {
                    dataRow[i] = columns[i];
                }
                catch
                {
                    Console.WriteLine("Unable to store value: " + columns[i] + " as column number " + i.ToString() + " does not exist.");
                }

            }

            //Console.WriteLine(dataRow.ToString());
            quickLexerTable.Rows.Add(dataRow);

            return quickLexerTable;

        }

        
        #endregion privateMethods
    }
    #endregion QuickLexerClass

    #region DelimitersForLexing
    public class DelimiterChars
    {
        public char columnSeparator;
        public char textIdentifier;
        public char newLineChar;

        public DelimiterChars(char columnSeparator, char textIdentifier, char newLineChar)
        {
            this.columnSeparator = columnSeparator;
            this.textIdentifier = textIdentifier;
            this.newLineChar = newLineChar;
        }
    }
    #endregion DelimitersForLexing

    public class QuickLexerLineData
    {
        public StreamReader reader;
        DelimiterChars delimiterChars;

        public QuickLexerLineData(StreamReader reader, DelimiterChars delimiterChars)
        {
            this.reader = reader;
            this.delimiterChars = delimiterChars;
        }

        #region newFunctionForColumnLexing
        public List<String> GetNextRowDataFromStreamReader()
        {
            string columnValue = "";
            string line = "";
            char nextDelimiter = this.delimiterChars.columnSeparator;
            char expectedDelimiter = this.delimiterChars.columnSeparator;
            List<String> columns = new List<String>();

            while (this.reader.Peek() > 0)
            {

                // append the next line of data to current data (which is empty if beginning)
                line += this.reader.ReadLine();

                while (line.Length > 0)//&& itCount < 20)
                {


                    //itCount++;
                    // Check to see what the next delimiter will be
                    nextDelimiter = GetNextDelimiter(line, this.delimiterChars);

                    // This section uses the line data, the columnvalue, and the delimiters to read the text into the columns array
                    // TODO: identify whether this should be it's own method and what it would return
                    //  idea - create custom object class for holding line, columnValue, and input delimiters - how would we handle expected and next delimiters?
                    #region LineToColumnLexing
                    // If the next Delimiter is a line break, but we are expecting a text identifier
                    // 1) Add the data to the column value, including the line break, and
                    // 2) get the next line of data
                    if (nextDelimiter == this.delimiterChars.newLineChar && expectedDelimiter == this.delimiterChars.textIdentifier)
                    {
                        columnValue += line;
                        columnValue += this.delimiterChars.newLineChar.ToString();
                        line = "";
                        break;
                    }
                    else if (expectedDelimiter == nextDelimiter && expectedDelimiter == this.delimiterChars.columnSeparator)
                    {
                        // If the expected and next Delimiter is the Separator,
                        //  1) throw the remaining substring into a columnValue holder, and
                        //  2) save to array, removing the Separator
                        columnValue += line.Remove(Math.Max(line.IndexOf(nextDelimiter), 0));
                        line = line.Substring(Math.Max(line.IndexOf(nextDelimiter), 0) + 1);
                        columns.Add(columnValue);
                        columnValue = "";
                        nextDelimiter = this.GetNextDelimiter(line, this.delimiterChars);
                    }
                    else if (expectedDelimiter == nextDelimiter && expectedDelimiter == this.delimiterChars.textIdentifier)
                    {

                        // If the expected Delimiter and the next Delimiter is the Text Identifier,
                        //  1) put the substring into column value,
                        //  2) set the next expected delimiter to the Text Separator,
                        //  3) but keep parsing data to column

                        columnValue += line.Remove(Math.Max(line.IndexOf(nextDelimiter), 0));
                        line = line.Substring(Math.Max(line.IndexOf(nextDelimiter), 0) + 1);
                        expectedDelimiter = this.delimiterChars.columnSeparator;
                    }
                    else if (nextDelimiter == this.delimiterChars.columnSeparator && expectedDelimiter == this.delimiterChars.textIdentifier)
                    {
                        // If the next Delimiter is the Text Separator, but we expect a text identifier, just keep parsing
                        columnValue += line.Remove(Math.Max(line.IndexOf(nextDelimiter), 0) + 1); //add the next bit to the column value (including the text-enclosed delimiter)
                        line = line.Substring(Math.Max(line.IndexOf(nextDelimiter), 0) + 1); //start parsing the line after the text-enclosed delimiter
                    }
                    else if (nextDelimiter == this.delimiterChars.textIdentifier && expectedDelimiter == this.delimiterChars.columnSeparator)
                    {
                        // If the next Delimiter is the Text Identifier, but we expect a text Separator,
                        // 1) put the data into the column,
                        // 2) change expected delimiter to text Identifier, and
                        // 3) keep parsing the data

                        columnValue += line.Remove(Math.Max(line.IndexOf(nextDelimiter), 0));
                        line = line.Substring(Math.Max(line.IndexOf(nextDelimiter), 0) + 1);
                        expectedDelimiter = this.delimiterChars.textIdentifier;
                    }

                    if (nextDelimiter == this.delimiterChars.newLineChar && expectedDelimiter == this.delimiterChars.columnSeparator)
                    {
                        // If the next Delimiter is a line break, but we would otherwise expect a Text Separator, then
                        // 1) Add the rest of the line to the column,
                        // 2) 
                        columnValue += line;
                        columns.Add(columnValue);
                        columnValue = "";
                        line = "";
                        //Console.WriteLine(columns);

                        return columns;
                        /*

                        columns.Clear();
                        */

                    }

                    #endregion LineToColumnLexing

                }
            }

            return columns;
        }

        #endregion newFunctionForColumnLexing
        private char GetNextDelimiter(String line, DelimiterChars delimiters)
        {
            int textIdentifierIndex = (line.IndexOf(delimiters.textIdentifier) < 0) ? line.Length + 1 : line.IndexOf(delimiters.textIdentifier);

            int columnSeparatorIndex = (line.IndexOf(delimiters.columnSeparator) < 0) ? line.Length + 1 : line.IndexOf(delimiters.columnSeparator);

            if (textIdentifierIndex < columnSeparatorIndex)
            {
                return delimiters.textIdentifier;
            }
            else if (textIdentifierIndex > columnSeparatorIndex)
            {
                return delimiters.columnSeparator;
            }
            else
            {
                return delimiters.newLineChar;
            }
        }
    }

    #region SqliteConnectionExtension
    //TODO: Implement new SqliteDataType class that instantiates based on the DataColumn Datatype
    /*
    public class SqliteDataColumn
    {
        public String SqliteDataType;
        public DataColumn DataColumn;
        // TODO: Implement Mapping pattern for data types

        public SqliteDataColumn(Type columnDataType, string columnName)
        {
            this.SqliteDataType = mapToSqliteDataType(columnDataType);
            this.DataColumn = new DataColumn(columnName);
        }

        private String mapToSqliteDataType(Type type)
        {
            //TODO: use the Type value to derive the SQL data type.
            return "VARCHAR(MAX)";
        }

        public override string ToString()
        {
            string columnString = "[" + this.DataColumn.ColumnName + "] " + this.SqliteDataType;
            return columnString;
        }

    }
    */
    //*
    public class ApplicationDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=Sharable;Mode=Memory;Cache=Shared");
            SQLitePCL.Batteries.Init();
        }
    }
    //*/
    
    #endregion SqliteConnectionExtension
   

}