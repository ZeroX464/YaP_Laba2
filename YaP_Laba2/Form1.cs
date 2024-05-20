using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using static System.Windows.Forms.AxHost;
using MathNet.Symbolics;
using System.Linq.Expressions;
using MathNet.Numerics;
using System.Runtime.Serialization;
using MathNet.Numerics.Differentiation;

namespace YaP_Laba2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                chart1.Series.Clear();
                Series series = new Series();
                series.ChartType = SeriesChartType.Line;
                reversePolishNotationLabel.Text = ReversePolishNotation.ToPolishNotation(textBox1.Text);
                label3.Text = ReversePolishNotation.Differentiate(textBox1.Text);

                int steps = 200;
                float stepSize = 0.1f;
                float startX = -10.0f;
                for (int i = 0; i <= steps; i++)
                {
                    float x = startX + i * stepSize;
                    double y = ReversePolishNotation.CalculatePolishNotation(ReversePolishNotation.ToPolishNotation(textBox1.Text.Replace("x", x.ToString("F2", CultureInfo.InvariantCulture))));
                    if (y != double.NaN && y != double.PositiveInfinity & y != double.NegativeInfinity)
                    {
                        series.Points.AddXY(x, y);
                    }
                    
                }
                chart1.Series.Add(series);
                
                double minY = series.Points.Min(p => p.YValues[0]);
                double maxY = series.Points.Max(p => p.YValues[0]);
                if (maxY != minY)
                {
                    chart1.ChartAreas[0].AxisY.Minimum = Math.Floor(minY);
                    chart1.ChartAreas[0].AxisY.Maximum = Math.Ceiling(maxY);
                }
                else
                {
                    chart1.ChartAreas[0].AxisY.Minimum = minY - 1;
                    chart1.ChartAreas[0].AxisY.Maximum = maxY + 1;
                }
            }
        }

        private void chart1_MouseClick(object sender, MouseEventArgs e)
        {
            HitTestResult result = chart1.HitTest(e.X, e.Y);
            if (result.ChartElementType == ChartElementType.PlottingArea ||
                result.ChartElementType == ChartElementType.DataPoint ||
                result.ChartElementType == ChartElementType.Gridlines)
            {
                if (chart1.Series.FindByName("TangentLine") != null)
                {
                    chart1.Series.RemoveAt(chart1.Series.IndexOf(chart1.Series.FindByName("TangentLine")));
                }

                double xValue = chart1.ChartAreas[0].AxisX.PixelPositionToValue(e.X);
                double yValue = chart1.ChartAreas[0].AxisY.PixelPositionToValue(e.Y);

                label4.Text = xValue.ToString("F2")+" "+yValue.ToString("F2");
                label2.Text = "Значение производной в точке: " + ReversePolishNotation.DifferentiateAT(label3.Text, xValue).ToString();

                var derivativeValue = ReversePolishNotation.DifferentiateAT(label3.Text, xValue);
                Series tangentLine = new Series("TangentLine");
                tangentLine.ChartType = SeriesChartType.Line;
                tangentLine.Points.AddXY(xValue - 1, yValue - derivativeValue);
                tangentLine.Points.AddXY(xValue + 1, yValue + derivativeValue);
                
                chart1.Series.Add(tangentLine);
            }
        }
    }
    public class ReversePolishNotation
    {
        static int Priority(char operation)
        {
            if (operation == 's' || operation == 'c')
                return 5;
            else if (operation == '~')
                return 4;
            else if (operation == '^')
                return 3;
            else if (operation == '*' || operation == '/')
                return 2;
            else if (operation == '+' || operation == '-')
                return 1;
            else
                return -1;
        }

        static bool Contains<T>(T[] arr, int size, T target)
        {
            return Array.IndexOf(arr, target) != -1;
        }

        public static string ToPolishNotation(string commonExpression)
        {
            System.Diagnostics.Debug.WriteLine(commonExpression);
            Stack<char> operationStack = new Stack<char>();
            string PolishNotation = "";
            char[] operationsSymbols = { '+', '-', '*', '/', '^', 's', 'c' };
            bool operationStackNotEmpty = false;

            Action putTopFromStackToNotation = () =>
            {
                PolishNotation += operationStack.Peek();
                if (operationStack.Peek() == 's') { PolishNotation += "in"; }
                if (operationStack.Peek() == 'c') { PolishNotation += "os"; }
                PolishNotation += " ";
                operationStack.Pop();
            };

            for (int i = 0; i < commonExpression.Length; i++)
            {
                char ch = commonExpression[i];
                if (char.IsDigit(ch) || ch == '.' || ch == 'x') // Операнд (цифра)
                {
                    PolishNotation += ch;
                    if (i != commonExpression.Length - 1 && !char.IsDigit(commonExpression[i + 1]) && commonExpression[i + 1] != '.') // Для пробела между числами
                    {
                        PolishNotation += " ";
                    }
                }
                else if (ch == '(')
                {
                    operationStack.Push(ch);
                }
                else if (ch == ')')
                {
                    while (operationStack.Count > 0 && operationStack.Peek() != '(') // Все операции до ( переносятся в конец польской записи
                    {
                        putTopFromStackToNotation();
                    }
                    operationStack.Pop(); // Удаление (
                    if (i == commonExpression.Length - 1) { PolishNotation = PolishNotation.Remove(PolishNotation.Length - 1); }
                }
                else if (ch == '-' && (i == 0 || commonExpression[i - 1] == '(' || !char.IsDigit(commonExpression[i - 1])))
                {
                    operationStack.Push('~'); // Унарный минус
                }
                else if (Contains(operationsSymbols, operationsSymbols.Length, ch)) // Операция
                {
                    if (ch == '^') // ^ - правоассоциативная операция
                    {
                        while (operationStack.Count > 0 && Priority(ch) < Priority(operationStack.Peek()))
                        {
                            putTopFromStackToNotation();
                        }
                    }
                    else
                    {
                        while (operationStack.Count > 0 && Priority(ch) <= Priority(operationStack.Peek())) // Если приоритет операции ниже или равен приоритету операции в стеке, то операции из стека заносятся в запись
                        {
                            putTopFromStackToNotation();
                        }
                    }
                    operationStack.Push(ch);
                    if (ch == 's' || ch == 'c') { i += 2; } // Пропуск 2 символов для sin, cos
                }
                else
                {
                    Console.WriteLine("При переводе в обратную польскую нотацию встречен некорректный символ");
                    return "";
                }
            }
            if (operationStack.Count > 0) { PolishNotation += " "; }
            while (operationStack.Count > 0) // Перевод остатка стека в конец польской записи после завершения чтения commonExpression
            {
                putTopFromStackToNotation();
                operationStackNotEmpty = true;
            }
            if (operationStackNotEmpty) { PolishNotation = PolishNotation.Remove(PolishNotation.Length - 1); } // Удаление лишнего пробела
            return PolishNotation;
        }

        public static float CalculatePolishNotation(string PolishNotation)
        {
            Stack<float> operandStack = new Stack<float>();
            char[] operations2 = { '+', '-', '*', '/', '^' };
            float operand1 = 0;
            float operand2 = 0;
            string number = ""; // Строка, чтобы хранить в ней цифры, пока они собираются в число

            for (int i = 0; i < PolishNotation.Length; i++)
            {
                char ch = PolishNotation[i];
                if (ch == ' ')
                {
                    if (number != "")
                    {
                        operandStack.Push(float.Parse(number, CultureInfo.InvariantCulture));
                        number = "";
                    }
                }
                else if (char.IsDigit(ch) || ch == '.') // Для чисел
                {
                    number += ch;
                }
                else // Для операций
                {
                    try
                    {
                        if (Contains(operations2, operations2.Length, ch)) // Операция с 2 операндами
                        {
                            if (operandStack.Count == 0)
                            {
                                throw new Exception("Стек операндов оказался пустым");
                            }

                            operand2 = operandStack.Pop();

                            if (operandStack.Count == 0)
                            {
                                throw new Exception("Стек операндов оказался пустым");
                            }

                            operand1 = operandStack.Pop();
                        }
                        else // Операция с 1 операндом
                        {
                            if (operandStack.Count == 0)
                            {
                                throw new Exception("Стек операндов оказался пустым");
                            }

                            operand1 = operandStack.Pop();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        return float.NaN;
                    }

                    switch (ch)
                    {
                        case '+':
                            operandStack.Push(operand1 + operand2);
                            break;
                        case '-':
                            operandStack.Push(operand1 - operand2);
                            break;
                        case '*':
                            operandStack.Push(operand1 * operand2);
                            break;
                        case '/':
                            if (Math.Abs(operand2) < Math.Pow(10, -10))
                            {
                                Console.WriteLine("Ошибка деления на 0");
                                return float.NaN;
                            }
                            operandStack.Push(operand1 / operand2);
                            break;
                        case '^':
                            operandStack.Push((float)Math.Pow(operand1, operand2));
                            break;
                        case '~':
                            operandStack.Push(-1 * operand1);
                            break;
                        case 's':
                            operandStack.Push((float)Math.Sin(operand1));
                            i += 2; // Пропуск символов i, n
                            break;
                        case 'c':
                            operandStack.Push((float)Math.Cos(operand1));
                            i += 2;
                            break;
                        default:
                            Console.WriteLine($"Такой операции нету: '{ch}'");
                            return float.NaN;
                    }
                }
            }
            if (number != "") { operandStack.Push(float.Parse(number, CultureInfo.InvariantCulture)); }
            number = "";

            if (operandStack.Count > 0)
            {
                float result = operandStack.Pop();
                System.Diagnostics.Debug.WriteLine(result);
                if (operandStack.Count == 0) { return result; }
                else
                {
                    Console.WriteLine("Обратная польская запись некорректна");
                    return float.NaN;
                }
            }
            else
            {
                Console.WriteLine("Стек оказался пустым");
                return 0;
            }
        }
        public static string Differentiate(string expressionString)
        {
            var x = SymbolicExpression.Variable("x");
            SymbolicExpression expression = SymbolicExpression.Parse(expressionString);
            var derivative = expression.Differentiate(x);
            return derivative.ToString();
        }
        public static double DifferentiateAT(string derivative, double value)
        {
            Console.WriteLine(derivative);
            var expression = SymbolicExpression.Parse(derivative);
            var dictionary = new Dictionary<string, FloatingPoint>();
            dictionary.Add("x", value);
            var result = expression.Evaluate(dictionary);
            if (result.IsReal)
            {
                Console.WriteLine("Результат выражения при x = " + value + " возвращает " + result.RealValue);
                return result.RealValue;
            }
            else
            {
                Console.WriteLine("Результат выражения при x = " + value + " возвращает нет данных");
                return double.NaN;
            }
        }
        /*
        static char[] expressionSymbols = { '+', '-', '*', '/', '^', 's', 'i', 'n', 'c', 'o', 's', '~', '.', ',', ' ', 'x' };
        static char[] operations1 = { 'n', 's', '~' };
        static char[] operations2 = { '+', '-', '*', '/', '^' };
        public static string TryDifferentiate(string expressionString)
        {
                for (int i = 0; i < expressionString.Length; i++)
                {
                    if (!expressionSymbols.Contains(expressionString[i]) && !char.IsDigit(expressionString[i]))
                    {
                        return "При нахождении производной встречен некорректный символ";
                    }
                }
                return TryDifferentiate2(expressionString);
        }
        public static string TryDifferentiate2(string expressionString)
        {
            if (double.TryParse(expressionString, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedNumber))
            {
                return "0";
            }
            else if (expressionString == "x") {
                return "1";
            }
            else if (expressionString.Last() == 'x')
            {
                return "1";
            }
            else if (char.IsDigit(expressionString.Last()))
            {
                return "0";
            }
            else // Строка с операцией
            {
                if (operations1.Contains(expressionString.Last())) // Операция с 1 аргументом
                {
                    switch (expressionString.Last())
                    {
                        
                        case 'n':
                            string stringWithoutSin = expressionString.Remove(expressionString.Length - 4);
                            return "cos(" + GetArguments(stringWithoutSin) + ") * " + TryDifferentiate2(stringWithoutSin); // sin(?)' = cos(?) * ?'
                        case 's':
                            string stringWithoutCos = expressionString.Remove(expressionString.Length - 4);
                            return "-sin(" + GetArguments(stringWithoutCos) + ") * " + TryDifferentiate2(stringWithoutCos);
                        case '~':
                            string stringWithoutUnaryMinus = expressionString.Remove(expressionString.Length - 2);
                            return "-(" + TryDifferentiate2(stringWithoutUnaryMinus) + ")";
                        default:
                            return "При нахождении производной получена ошибка";
                    }
                }
                else
                {
                    string stringWithoutOperations2 = expressionString.Remove(expressionString.Length - 2);
                    switch (expressionString.Last())
                    {
                        case '+':
                            return TryDifferentiate2(stringWithoutOperations2.Remove(GetIndexForLeftArg(stringWithoutOperations2))) + " + " + TryDifferentiate2(stringWithoutOperations2);
                        case '-':
                            return TryDifferentiate2(stringWithoutOperations2.Remove(GetIndexForLeftArg(stringWithoutOperations2))) + " - " + TryDifferentiate2(stringWithoutOperations2);
                        case '*':
                            return TryDifferentiate2(stringWithoutOperations2.Remove(GetIndexForLeftArg(stringWithoutOperations2))) + " * " + GetArguments(stringWithoutOperations2) + " + " + GetArguments(stringWithoutOperations2.Remove(GetIndexForLeftArg(stringWithoutOperations2))) + " * " + TryDifferentiate2(stringWithoutOperations2);
                        case '/':
                            return TryDifferentiate2(stringWithoutOperations2.Remove(GetIndexForLeftArg(stringWithoutOperations2))) + " * " + GetArguments(stringWithoutOperations2) + " - " + GetArguments(stringWithoutOperations2.Remove(GetIndexForLeftArg(stringWithoutOperations2))) + " * " + TryDifferentiate2(stringWithoutOperations2) + " / " + GetArguments(stringWithoutOperations2) + "^2";
                        case '^':
                            string rightArg = GetArguments(stringWithoutOperations2);
                            string leftArg = GetArguments(stringWithoutOperations2.Remove(GetIndexForLeftArg(stringWithoutOperations2)));
                            if (double.TryParse(leftArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var _) && // Число в степени числа
                                double.TryParse(rightArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var _))
                            {
                                return "0";
                            }
                            else if (double.TryParse(leftArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out _) && // Число в чём-то с x
                                     !double.TryParse(rightArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var _))
                            {
                                return leftArg + " ^ " + rightArg + " ln(" + leftArg + ")";
                            }
                            else if (!double.TryParse(leftArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out _) && // x в степени числа
                                     double.TryParse(rightArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedRightArg))
                            {
                                return rightArg + " * " + leftArg + " ^ " + (parsedRightArg - 1).ToString();
                            }
                            else // x^x
                            { // a(x)^b(x) = a^b * a'ln(b) + a/b
                                return leftArg + " ^ " + rightArg + " * " + TryDifferentiate2(stringWithoutOperations2.Remove(GetIndexForLeftArg(stringWithoutOperations2))) + " * " + "ln(" + rightArg + ") + " + leftArg + " / " + rightArg;
                            }
                        default:
                            return "При нахождении производной получена ошибка";


                    }
                }
            }
        }
        public static string GetArguments(string expressionString)
        {
            if (double.TryParse(expressionString, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedNumber))
            {
                return expressionString;
            }
            else if (expressionString == "x")
            {
                return "x";
            }
            else if (expressionString.Last() == 'x' || char.IsDigit(expressionString.Last()))
            {
                String[] strings = expressionString.Split(' ');
                return strings[strings.Length - 1];
            }
            else // Строка с операцией
            {
                if (operations1.Contains(expressionString.Last())) // Операция с 1 аргументом
                {
                    switch (expressionString.Last())
                    {
                        case 'n':
                            string sinArg = GetArguments(expressionString.Remove(expressionString.Length - 4));
                            if (double.TryParse(sinArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedSinArg)) // Число
                            {
                                return Math.Sin(parsedSinArg).ToString();
                            }
                            else // Что-то с x
                            {
                                return "sin(" + sinArg + ")";
                            }
                        case 's':
                            string cosArg = GetArguments(expressionString.Remove(expressionString.Length - 4));
                            if (!double.TryParse(cosArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedCosArg))
                            {
                                return Math.Cos(parsedCosArg).ToString();
                            }
                            else
                            {
                                return "cos(" + cosArg + ")";
                            }
                        case '~':
                            string unArg = GetArguments(expressionString.Remove(expressionString.Length - 2));
                            if (double.TryParse(unArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedUnArg)) // Число
                            {
                                return (-parsedUnArg).ToString();
                            }
                            else // Что-то с x
                            {
                                return "-(" + parsedUnArg + ")";
                            }
                    }
                }
                else // Операция с 2 аргументами
                {
                    string rightArg = GetArguments(expressionString.Remove(expressionString.Length - 2));
                    string leftArg = GetArguments(expressionString.Remove(GetIndexForLeftArg(expressionString))); // индекс на котором закончился leftPlus
                    switch (expressionString.Last())
                    {
                        case '+':
                            if (double.TryParse(rightArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedRightPlusArg) &&
                                double.TryParse(leftArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedLeftPlusArg)) // Числа
                            {
                                return (parsedLeftPlusArg + parsedRightPlusArg).ToString();
                            }
                            else // Что-то с x
                            {
                                return leftArg + " + " + rightArg;
                            }
                        case '-':
                            if (double.TryParse(rightArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedRightMinusArg) &&
                                double.TryParse(leftArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedLeftMinusArg)) // Числа
                            {
                                return (parsedLeftMinusArg - parsedRightMinusArg).ToString();
                            }
                            else // Что-то с x
                            {
                                return leftArg + " - " + rightArg;
                            }
                        case '*':
                            if (double.TryParse(rightArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedRightMultiplyArg) &&
                                double.TryParse(leftArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedLeftMultiplyArg)) // Числа
                            {
                                return (parsedLeftMultiplyArg * parsedRightMultiplyArg).ToString();
                            }
                            else // Что-то с x
                            {
                                return leftArg + " * " + rightArg;
                            }
                        case '/':
                            if (double.TryParse(rightArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedRightDivideArg) &&
                                double.TryParse(leftArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedLeftDivideArg)) // Числа
                            {
                                return (parsedLeftDivideArg / parsedRightDivideArg).ToString();
                            }
                            else // Что-то с x
                            {
                                return leftArg + " / " + rightArg;
                            }
                        case '^':
                            if (double.TryParse(rightArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedRightPowArg) &&
                                double.TryParse(leftArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedLeftPowArg)) // Числа
                            {
                                return (Math.Pow(parsedLeftPowArg, parsedRightPowArg)).ToString();
                            }
                            else // Что-то с x
                            {
                                return leftArg + " ^ " + rightArg;
                            }

                    }
                }
            }
            return null;
        }
        public static int GetIndexForLeftArg(string expressionString) // Выдаёт индекс последнего необходимого аргумента и ещё на 1 влево чтобы убрать пробел
        {
            if (double.TryParse(expressionString, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedNumber))
            {
                return 0;
            }
            else if (expressionString == "x")
            {
                return 0;
            }
            else if (expressionString.Last() == 'x' || char.IsDigit(expressionString.Last()))
            {
                return expressionString.LastIndexOf(' ');
            }
            else // Строка с операцией
            {
                if (operations1.Contains(expressionString.Last())) // Операция с 1 аргументом
                {
                    switch (expressionString.Last())
                    {
                        case 'n':
                        case 's':
                            return GetIndexForLeftArg(expressionString.Remove(expressionString.Length - 4));
                        case '~':
                            return GetIndexForLeftArg(expressionString.Remove(expressionString.Length - 2));
                    }
                }
                else // Операция с 2 аргументами
                {
                    //int leftArgIndex = GetIndexForLeftArg(expressionString.Remove(expressionString.Length - 2));
                    int rightArgIndex = GetIndexForLeftArg(expressionString.Remove(expressionString.Length - 2));
                    return rightArgIndex;
                }
            }
            return -1;
        }
        */
    }
}
