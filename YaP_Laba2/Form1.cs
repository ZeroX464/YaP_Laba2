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
                label3.Text = ReversePolishNotation.TryDifferentiate(reversePolishNotationLabel.Text);

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
                string derivative;
                if (xValue < 0)
                {
                    derivative = label3.Text.Replace("x", "(" + xValue.ToString() + ")").Replace(',', '.');
                }
                else
                {
                    derivative = label3.Text.Replace("x", xValue.ToString()).Replace(',', '.');
                }
                string derivativeInPolishNotation = ReversePolishNotation.ToPolishNotation(derivative);
                Console.WriteLine("Производная в польской нотации: " + derivativeInPolishNotation);
                float derivativeValue = ReversePolishNotation.CalculatePolishNotation(derivativeInPolishNotation);
                label2.Text = "Значение производной в точке: " + derivativeValue.ToString("F2");

                Series tangentLine = new Series("TangentLine");
                tangentLine.ChartType = SeriesChartType.Line;
                tangentLine.BorderWidth = 3;
                float findTangent_y(double x)
                {
                    return ReversePolishNotation.CalculatePolishNotation(ReversePolishNotation.ToPolishNotation(textBox1.Text.Replace("x", x.ToString(CultureInfo.InvariantCulture))));
                }
                float y_0 = findTangent_y(xValue);
                label5.Text = "Точка касания: " + xValue.ToString("F2") + " " + y_0.ToString("F2");
                float tangent_y_1 = y_0 + derivativeValue * (-10 - (float)xValue);
                float tangent_y_2 = y_0 + derivativeValue * (10 - (float)xValue);
                tangentLine.Points.AddXY(-10, tangent_y_1);
                tangentLine.Points.AddXY(10, tangent_y_2);
                
                chart1.Series.Add(tangentLine);
            }
        }
    }
    public class ReversePolishNotation
    {
        static int Priority(char operation)
        {
            if (operation == 's' || operation == 'c' || operation == 'l')
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
            char[] operationsSymbols = { '+', '-', '*', '/', '^', 's', 'c', 'l' };
            bool operationStackNotEmpty = false;

            Action putTopFromStackToNotation = () =>
            {
                PolishNotation += operationStack.Peek();
                if (operationStack.Peek() == 's') { PolishNotation += "in"; }
                if (operationStack.Peek() == 'c') { PolishNotation += "os"; }
                if (operationStack.Peek() == 'l') { PolishNotation += "n"; }
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
                else if (ch == '-' && (i == 0 || commonExpression[i - 1] == '(' || (!char.IsDigit(commonExpression[i - 1]) && !(commonExpression[i - 1] == 'x') && !(commonExpression[i - 1] == ')'))))
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
                    if (ch == 'l') { i++; } // Для ln
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
                        case 'l':
                            if (operand1 > 0 && operand1 != 1)
                            {
                                operandStack.Push((float)Math.Log(operand1));
                                i++;
                            }
                            else
                            {
                                Console.WriteLine("Ошибка при вычислении логарифма");
                                return float.NaN;
                            }
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
            if (char.IsDigit(expressionString.Last()))
            {
                return "0";
            }
            else if (expressionString.Last() == 'x')
            {
                return "1";
            }
            else // Строка с операцией
            {
                char lastChar = expressionString.Last();
                string stringWithoutLastOperation = expressionString.Remove(expressionString.LastIndexOf(' '));
                if (operations1.Contains(expressionString.Last())) // Операция с 1 аргументом
                {
                    switch (lastChar)
                    {
                        case 'n':
                            return "cos(" + GetArguments(stringWithoutLastOperation) + ")*("+ TryDifferentiate2(stringWithoutLastOperation) + ")"; // sin(?)' = cos(?) * ?'
                        case 's':
                            return "-sin(" + GetArguments(stringWithoutLastOperation) + ")*(" + TryDifferentiate2(stringWithoutLastOperation) + ")";
                        case '~':
                            return "-(" + TryDifferentiate2(stringWithoutLastOperation) + ")";
                        default:
                            return "При нахождении производной получена ошибка";
                    }
                }
                else // Операция с 2 аргументами
                {
                    string rightArg = GetArguments(stringWithoutLastOperation);
                    string leftArg = GetArguments(stringWithoutLastOperation.Remove(SkipArgumentAndGetIndex(stringWithoutLastOperation)));
                    string derivativeOfRightArg = TryDifferentiate2(stringWithoutLastOperation);
                    string derivativeOfLeftArg = TryDifferentiate2(stringWithoutLastOperation.Remove(SkipArgumentAndGetIndex(stringWithoutLastOperation)));
                    switch (lastChar)
                    {
                        case '+':
                            return derivativeOfLeftArg + "+" + derivativeOfRightArg;
                        case '-':
                            return derivativeOfLeftArg + "-(" + derivativeOfRightArg + ")";
                        case '*':
                            return "(" + derivativeOfLeftArg + ")*(" + rightArg + ")+(" + leftArg + ")*(" + derivativeOfRightArg + ")";
                        case '/':
                            return "((" + derivativeOfLeftArg + ")*(" + rightArg + ")-(" + leftArg + ")*(" + derivativeOfRightArg + "))/((" + rightArg + ")^2)";
                        case '^':
                            if (double.TryParse(leftArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var _) && // Число в степени числа
                                double.TryParse(rightArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var _))
                            {
                                return "0";
                            }
                            else if (double.TryParse(leftArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out _) && // Число в чём-то с x
                                     !double.TryParse(rightArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var _))
                            {
                                return leftArg + "^" + rightArg + "*" + " ln(" + leftArg + ")";
                            }
                            else if (!double.TryParse(leftArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out _) && // x в степени числа
                                     double.TryParse(rightArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedRightArg))
                            {
                                return rightArg + "*" + leftArg + "^" + (parsedRightArg - 1).ToString();
                            }
                            else // x^x
                            { // a(x)^b(x) = a^b * b'ln(a) + b/a
                                return "(" + leftArg + ")" + "^" + "(" + rightArg + ")" + "*" + "(" + derivativeOfRightArg + ")" + "*" + "ln(" + leftArg + ")+" + "(" + rightArg + ")" + "/" + "(" + leftArg + ")";
                            }
                        default:
                            return "При нахождении производной получена ошибка";


                    }
                }
            }
        }
        public static string GetArguments(string expressionString)
        {
            if (expressionString.Last() == 'x' || char.IsDigit(expressionString.Last()))
            {
                String[] strings = expressionString.Split(' ');
                return strings[strings.Length - 1];
            }
            else // Строка с операцией
            {
                char lastChar = expressionString.Last();
                string stringWithoutLastOperation = expressionString.Remove(expressionString.LastIndexOf(' '));
                string rightArg = GetArguments(stringWithoutLastOperation);
                if (operations1.Contains(lastChar)) // Операция с 1 аргументом
                {
                    switch (lastChar)
                    {
                        case 'n':
                            if (double.TryParse(rightArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedSinArg)) // Число
                            {
                                double sinResult = Math.Sin(parsedSinArg);
                                if (sinResult >= 0)
                                {
                                    return sinResult.ToString();
                                }
                                else
                                {
                                    return "(" + sinResult.ToString() + ")";
                                }
                            }
                            else // Что-то с x
                            {
                                return "sin(" + rightArg + ")";
                            }
                        case 's':
                            if (double.TryParse(rightArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedCosArg))
                            {
                                double cosResult = Math.Cos(parsedCosArg);
                                if (cosResult >= 0)
                                {
                                    return cosResult.ToString();
                                }
                                else
                                {
                                    return "(" + cosResult.ToString() + ")";
                                }
                            }
                            else
                            {
                                return "cos(" + rightArg + ")";
                            }
                        case '~':
                            if (double.TryParse(rightArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedUnArg)) // Число
                            {
                                if (-parsedUnArg < 0)
                                {
                                    return "(" + (-parsedUnArg).ToString() + ")";
                                }
                                else
                                {
                                    return (-parsedUnArg).ToString();
                                }
                            }
                            else // Что-то с x
                            {
                                return "(-(" + rightArg + "))";
                            }
                    }
                }
                else // Операция с 2 аргументами
                {
                    string leftArg = GetArguments(expressionString.Remove(SkipArgumentAndGetIndex(stringWithoutLastOperation)));
                    switch (lastChar)
                    {
                        case '+':
                            if (double.TryParse(rightArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedRightPlusArg) &&
                                double.TryParse(leftArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedLeftPlusArg)) // Числа
                            {
                                if (parsedLeftPlusArg + parsedRightPlusArg < 0)
                                {
                                    return "(" + (parsedLeftPlusArg + parsedRightPlusArg).ToString() + ")";
                                }
                                else
                                {
                                    return (parsedLeftPlusArg + parsedRightPlusArg).ToString();
                                }
                            }
                            else // Что-то с x
                            {
                                return leftArg + "+" + rightArg;
                            }
                        case '-':
                            if (double.TryParse(rightArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedRightMinusArg) &&
                                double.TryParse(leftArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedLeftMinusArg)) // Числа
                            {
                                if (parsedLeftMinusArg - parsedRightMinusArg < 0)
                                {
                                    return "(" + (parsedLeftMinusArg - parsedRightMinusArg).ToString() + ")";
                                }
                                else
                                {
                                    return (parsedLeftMinusArg - parsedRightMinusArg).ToString();
                                }
                            }
                            else // Что-то с x
                            {
                                if (rightArg == "x") { return leftArg + "-" + rightArg; }
                                else { return leftArg + "-(" + rightArg + ")"; }
                            }
                        case '*':
                            if (double.TryParse(rightArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedRightMultiplyArg) &&
                                double.TryParse(leftArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedLeftMultiplyArg)) // Числа
                            {
                                if (parsedLeftMultiplyArg * parsedRightMultiplyArg < 0)
                                {
                                    return "(" + (parsedLeftMultiplyArg * parsedRightMultiplyArg).ToString() + ")";
                                }
                                else
                                {
                                    return (parsedLeftMultiplyArg * parsedRightMultiplyArg).ToString();
                                }
                            }
                            else // Что-то с x
                            {
                                if (leftArg == "x" && rightArg == "x") { return leftArg + "*" + rightArg; }
                                else if (leftArg == "x") { return leftArg + "*(" + rightArg + ")"; }
                                else if (rightArg == "x") { return "(" + leftArg + ")*" + rightArg; }
                                else { return "(" + leftArg + ")*(" + rightArg + ")"; }
                            }
                        case '/':
                            if (double.TryParse(rightArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedRightDivideArg) &&
                                double.TryParse(leftArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedLeftDivideArg)) // Числа
                            {
                                if (parsedLeftDivideArg / parsedRightDivideArg < 0)
                                {
                                    return "(" + (parsedLeftDivideArg / parsedRightDivideArg).ToString() + ")";
                                }
                                else
                                {
                                    return (parsedLeftDivideArg / parsedRightDivideArg).ToString();
                                }
                            }
                            else // Что-то с x
                            {
                                if (leftArg == "x" && rightArg == "x") { return leftArg + "/" + rightArg; }
                                else if (leftArg == "x") { return leftArg + "/(" + rightArg + ")"; }
                                else if (rightArg == "x") { return "(" + leftArg + ")/" + rightArg; }
                                else { return "(" + leftArg + ")/(" + rightArg + ")"; }
                            }
                        case '^':
                            if (double.TryParse(rightArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedRightPowArg) &&
                                double.TryParse(leftArg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedLeftPowArg)) // Числа
                            {
                                if (Math.Pow(parsedLeftPowArg, parsedRightPowArg) < 0)
                                {
                                    return "(" + (Math.Pow(parsedLeftPowArg, parsedRightPowArg)).ToString() + ")";
                                }
                                else
                                {
                                    return (Math.Pow(parsedLeftPowArg, parsedRightPowArg)).ToString();
                                }
                            }
                            else // Что-то с x
                            {
                                if (leftArg == "x" && rightArg == "x") { return leftArg + "^" + rightArg; }
                                else if (leftArg == "x") { return leftArg + "^(" + rightArg + ")"; }
                                else if (rightArg == "x") { return "(" + leftArg + ")^" + rightArg; }
                                else { return "(" + leftArg + ")^(" + rightArg + ")"; }
                            }

                    }
                }
            }
            return null;
        }
        public static int SkipArgumentAndGetIndex(string expressionString)
        {
            char lastChar = expressionString.Last();
            if (lastChar == 'x' || char.IsDigit(lastChar)) // Число или x
            {
                return expressionString.LastIndexOf(' ');
            }
            else if (operations1.Contains(lastChar)) // Операция из 1 стека
            {
                string stringWithoutLastOperation = expressionString.Remove(expressionString.LastIndexOf(' '));
                int indexOfSpaceBeforeArgument = SkipArgumentAndGetIndex(stringWithoutLastOperation);
                return indexOfSpaceBeforeArgument;
            }
            else // Операция из 2 стека
            {
                string stringWithoutLastOperation = expressionString.Remove(expressionString.LastIndexOf(' '));
                int indexOfSpaceBeforeRightArgument = SkipArgumentAndGetIndex(stringWithoutLastOperation);
                string stringWithoutRightArgument = stringWithoutLastOperation.Remove(indexOfSpaceBeforeRightArgument);
                int indexOfSpaceBeforeLeftArgument = SkipArgumentAndGetIndex(stringWithoutRightArgument);
                return indexOfSpaceBeforeLeftArgument;
            }
        }
    }
}
