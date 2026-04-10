using System.Collections.Generic;
using UnityEngine;
using TMPro;

// This Single script handles everything from input, evaluation and display.
public class CalculatorController : MonoBehaviour
{
    [SerializeField] private TMP_Text expressionText;

    private string currentExpr = "";
    private bool justEvaluated = false;

    private char[] operators = { '+', '-', '*', '/' };

    // Start is called before the first frame update
    void Start()
    {
        expressionText.text = "";
    }

    // All number or operator buttons will call this function
    public void OnButtonPressed(string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        // Map display symbols to internal operators(x and ÷)
        if (value == "×") value = "*";
        else if (value == "÷") value = "/";

        if (IsOperator(value[0]))
        {
            HandleOperator(value[0]);
        }
        else
        {
            HandleNumber(value);
        }

        expressionText.text = ToDisplayExpr(currentExpr);
    }

    private void HandleNumber(string val)
    {
        if (justEvaluated)
        {
            currentExpr = "";
            justEvaluated = false;
        }

        // Prevent more than one decimal in the current number segment
        if (val == ".")
        {
            string lastNum = GetLastNumber();

            if (lastNum.Contains(".")) return;
        }

        // Prevent leading zeros like "007" but allow "0."
        if (val == "0" || val == "00")
        {
            string lastNum = GetLastNumber();
            if (lastNum == "0") return;
        }

        currentExpr += val;
    }

    private void HandleOperator(char op)
    {
        justEvaluated = false;

        if (currentExpr.Length == 0)
        {
            // Allow starting with minus (negative numbers)
            if (op == '-') currentExpr += op;
            return;
        }

        char last = currentExpr[currentExpr.Length - 1];

        // Replace last operator if user hits two in a row
        if (IsOperator(last))
        {
            currentExpr = currentExpr.Substring(0, currentExpr.Length - 1);
        }

        // Also strip trailing decimal before operator
        if (last == '.')
        {
            currentExpr = currentExpr.Substring(0, currentExpr.Length - 1);
        }

        currentExpr += op;
    }


    public void OnEquals()
    {
        if (currentExpr.Length == 0) return;

        char last = currentExpr[currentExpr.Length - 1];
        if (IsOperator(last) || last == '.')
            currentExpr = currentExpr.Substring(0, currentExpr.Length - 1);

        float result = EvaluateExpression(currentExpr);

        if (float.IsNaN(result) || float.IsInfinity(result))
        {
            currentExpr = "";
            expressionText.text = "Error";
        }
        else
        {
            currentExpr = FormatResult(result);
            expressionText.text = ToDisplayExpr(currentExpr);
        }

        justEvaluated = true;
    }

    public void OnClear()
    {
        currentExpr = "";
        expressionText.text = "";
    }

    public void OnBackspace()
    {
        if (currentExpr.Length == 0) return;
        currentExpr = currentExpr.Substring(0, currentExpr.Length - 1);
        expressionText.text = ToDisplayExpr(currentExpr);
    }

    // This function evaluates expression based on DMAS rules.
    // Handle * and / first, then + and -
    private float EvaluateExpression(string expr)
    {
        List<string> tokens = Tokenize(expr);
        if (tokens == null || tokens.Count == 0) return 0f;

        // First: multiply and divide left to right
        int i = 0;
        while (i < tokens.Count)
        {
            if (tokens[i] == "*" || tokens[i] == "/")
            {
                if (i == 0 || i >= tokens.Count - 1) return float.NaN;

                float left = ParseNum(tokens[i - 1]);
                float right = ParseNum(tokens[i + 1]);
                float res;

                if (tokens[i] == "*")
                    res = left * right;
                else
                {
                    if (right == 0f) return float.NaN; // div by zero
                    res = left / right;
                }

                // Collapse these 3 tokens into one result
                tokens[i - 1] = res.ToString("R");
                tokens.RemoveAt(i);     // remove operator
                tokens.RemoveAt(i);     // remove right operand
            }
            else
            {
                i++;
            }
        }

        // Second: addition and subtraction left to right
        if (!float.TryParse(tokens[0], out float acc)) return float.NaN;

        i = 1;
        while (i < tokens.Count - 1)
        {
            string op = tokens[i];
            float next = ParseNum(tokens[i + 1]);

            if (op == "+") acc += next;
            else if (op == "-") acc -= next;
            else return float.NaN; // shouldn't happen

            i += 2;
        }

        return acc;
    }

    // Split expression string into list of number strings and operator strings
    private List<string> Tokenize(string expr)
    {
        List<string> tokens = new();
        string current = "";

        for (int i = 0; i < expr.Length; i++)
        {
            char c = expr[i];

            // Handle negative sign at start or after operator
            if (c == '-' && (i == 0 || IsOperator(expr[i - 1])))
            {
                current += c;
                continue;
            }

            if (IsOperator(c))
            {
                if (current.Length > 0)
                {
                    tokens.Add(current);
                    current = "";
                }
                tokens.Add(c.ToString());
            }
            else
            {
                current += c;
            }
        }

        if (current.Length > 0)
            tokens.Add(current);

        return tokens;
    }

#region Helper Functions

    // Checks if char is one of our operators
    private bool IsOperator(char c)
    {
        foreach (char op in operators)
            if (c == op) return true;
        return false;
    }

    private float ParseNum(string s)
    {
        if (float.TryParse(s, out float val)) return val;
        return float.NaN;
    }

    // Gets the last number in the expression
    private string GetLastNumber()
    {
        string seg = "";
        for (int i = currentExpr.Length - 1; i >= 0; i--)
        {
            char c = currentExpr[i];
            if (IsOperator(c) && i != 0) break; // stop at operator
            seg = c + seg;
        }
        return seg;
    }

    // Formats result for display
    private string FormatResult(float val)
    {
        // If it's a whole number, don't show decimal
        if (val == Mathf.Floor(val) && Mathf.Abs(val) < 1e9f)
            return ((long)val).ToString();

        // Otherwise round to avoid floating point ugliness like 2.9999998
        return Mathf.Round(val * 1000000f) / 1000000f + "";
    }

    private string ToDisplayExpr(string expr)
    {
        return expr.Replace("*", "×").Replace("/", "÷");
    }

#endregion
}