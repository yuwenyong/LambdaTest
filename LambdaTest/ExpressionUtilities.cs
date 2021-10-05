using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LambdaTest
{
    public class DBFilterParameter
    {
        public string Name { get; set; }

        public Type Type { get; set; }

        public object Value { get; set; }
    }

    public class DBFilterStatement
    {
        private string _queryText;

        public IDBQueryFilter Filter { get; set; }

        public string QueryText
        {
            get
            {
                if (string.IsNullOrEmpty(_queryText))
                {
                    _queryText = this.Filter.GetQueryText();
                }
                return _queryText;
            }
        }

        public IList<DBFilterParameter> Parameters { get; set; }
    }

    public class DBFilterBuildContext
    {
        public string ParameterName { get; set; }
        public IDictionary<string, int> ParameterUsed { get; set; }
    }

    public interface IDBQueryParameter
    {
        string GetQueryText();

        bool IsNullValue { get; }
    }

    public class DBMemberAccessParameter: IDBQueryParameter
    {
        public string GetQueryText()
        {
            return this.MemberName;
        }

        public bool IsNullValue { get => false; }

        public string MemberName { get; set; }
    }

    public class DBConstantParameter: IDBQueryParameter
    {
        public string GetQueryText()
        {
            if (!string.IsNullOrEmpty(this.ParameterName))
            {
                return this.ParameterName;
            }
            return this.Value == null ? "NULL" : this.Value.ToString();
        }

        public bool IsNullValue { get => Value is null; }

        public Type Type { get; set; }
        public object Value { get; set; }

        public string ParameterName { get; set; }
    }

    public interface IDBQueryFilter
    {
        bool IsCompoundFilter { get; }
        string GetQueryText();
    }

    public enum DBBinaryLogicOperator
    {
        And,
        Or
    }

    public enum DBUnaryLogicOperator
    {
        Not
    }

    public class DBUnaryLogicFilter: IDBQueryFilter
    {
        public DBUnaryLogicOperator Operator { get; set; }

        public bool IsCompoundFilter
        {
            get => true;
        }

        public IDBQueryFilter Operand { get; set; }

        public string GetQueryText()
        {
            string query = (this.Operand.IsCompoundFilter ? "(" + this.Operand.GetQueryText() + ")" : this.Operand.GetQueryText());

            switch (this.Operator)
            {
                case DBUnaryLogicOperator.Not:
                    return "NOT " + query;
                default:
                    throw new Exception("Unexpected DBUnaryLogicOperator");
            }
        }
    }

    public class DBBinaryLogicFilter: IDBQueryFilter
    {
        public DBBinaryLogicOperator Operator { get; set; }

        public bool IsCompoundFilter
        {
            get => true;
        }

        public IDBQueryFilter Left { get; set; }

        public IDBQueryFilter Right { get; set; }

        public string GetQueryText()
        {
            string leftQuery = (this.Left.IsCompoundFilter ? "(" + this.Left.GetQueryText() + ")" : this.Left.GetQueryText());
            string rightQuery = (this.Right.IsCompoundFilter ? "(" + this.Right.GetQueryText() + ")" : this.Right.GetQueryText()); ;

            switch (this.Operator)
            {
                case DBBinaryLogicOperator.And:
                    return leftQuery + " AND " + rightQuery;
                case DBBinaryLogicOperator.Or:
                    return leftQuery + " OR " + rightQuery;
                default:
                    throw new Exception("Unexpected DBBinaryLogicOperator");
            }
        }
    }

    public enum DBBinaryComparisionOperator
    {
        Equal,
        NotEqual,
        GreaterThan,
        GreaterEqual,
        LessThan,
        LessEqual,
        // Has
        // In
        Contains,
        EndsWith,
        StartsWith,
    }

    public class DBBinaryComparisionFilter : IDBQueryFilter
    {
        public DBBinaryComparisionOperator Operator { get; set; }

        public bool IsCompoundFilter
        {
            get => false;
        }

        public IDBQueryParameter Left { get; set; }

        public IDBQueryParameter Right { get; set; }

        public string GetQueryText()
        {
            switch (this.Operator)
            {
                case DBBinaryComparisionOperator.Equal:
                    if (this.Left.IsNullValue)
                    {
                        return this.Right.GetQueryText() + " IS NULL";
                    }
                    else if (this.Right.IsNullValue)
                    {
                        return this.Left.GetQueryText() + " IS NULL";
                    }
                    else
                    {
                        return this.Left.GetQueryText() + " = " + this.Right.GetQueryText();
                    }
                case DBBinaryComparisionOperator.NotEqual:
                    if (this.Left.IsNullValue)
                    {
                        return this.Right.GetQueryText() + " IS NOT NULL";
                    }
                    else if (this.Right.IsNullValue)
                    {
                        return this.Left.GetQueryText() + " IS NOT NULL";
                    }
                    else
                    {
                        return this.Left.GetQueryText() + " <> " + this.Right.GetQueryText();
                    }
                case DBBinaryComparisionOperator.GreaterThan:
                    return this.Left.GetQueryText() + " > " + this.Right.GetQueryText();
                case DBBinaryComparisionOperator.GreaterEqual:
                    return this.Left.GetQueryText() + " >= " + this.Right.GetQueryText();
                case DBBinaryComparisionOperator.LessThan:
                    return this.Left.GetQueryText() + " < " + this.Right.GetQueryText();
                case DBBinaryComparisionOperator.LessEqual:
                    return this.Left.GetQueryText() + " <= " + this.Right.GetQueryText();
                case DBBinaryComparisionOperator.Contains:
                    return "LIKE('%' || " + this.Right.GetQueryText() + " || '%', " + this.Left.GetQueryText() + ")";
                case DBBinaryComparisionOperator.StartsWith:
                    return "LIKE(" + this.Right.GetQueryText() + " || '%', " + this.Left.GetQueryText() + ")";
                case DBBinaryComparisionOperator.EndsWith:
                    return "LIKE('%' || " + this.Right.GetQueryText() + ", " + this.Left.GetQueryText() + ")";
                default:
                    throw new Exception("Unexpected DBBinaryComparisionOperator");
            }
        }
    }

    
}
