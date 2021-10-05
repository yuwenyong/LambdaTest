using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace LambdaTest
{
    public class DBFilterTranslator
    {
        DBFilterStatement _statment;
        DBFilterBuildContext _context;

        public DBFilterStatement Translate(Expression expression)
        {
            if (expression == null)
            {
                return null;
            }

            _statment = new DBFilterStatement
            {
                Parameters = new List<DBFilterParameter>()
            };
            _context = new DBFilterBuildContext
            {
                ParameterUsed = new Dictionary<string, int>()
            };

            if (expression.NodeType != ExpressionType.Lambda)
            {
                throw new Exception("Lambda Expression expected");
            }
            _statment.Filter = this.VisitLambda((LambdaExpression)expression);

            return _statment;
        }

        protected IDBQueryFilter TranslateExpression(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Not:
                    return this.VisitUnary((UnaryExpression)expression);
                case ExpressionType.AndAlso:
                case ExpressionType.OrElse:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                    return this.VisitBinary((BinaryExpression)expression);
                case ExpressionType.Call:
                    return this.VisitMethodCall((MethodCallExpression)expression);
                default:
                    throw new Exception("Expression expected");
            }
        }

        protected IDBQueryFilter VisitLambda(LambdaExpression expression)
        {
            if (expression.Parameters.Count != 1)
            {
                throw new Exception("Only one parameter is allowed");
            }
            _context.ParameterName = expression.Parameters[0].Name;
            return this.TranslateExpression(expression.Body);
        }

        protected IDBQueryFilter VisitUnary(UnaryExpression expression)
        {
            if (expression.NodeType == ExpressionType.Not)
            {
                return new DBUnaryLogicFilter
                {
                    Operator = DBUnaryLogicOperator.Not,
                    Operand = this.TranslateExpression(expression.Operand)
                };
            }
            else
            {
                return null;
            }
        }

        protected IDBQueryFilter VisitBinary(BinaryExpression expression)
        {
            IDBQueryFilter filter = null;
            switch (expression.NodeType)
            {
                case ExpressionType.AndAlso:
                    filter = new DBBinaryLogicFilter
                    {
                        Operator = DBBinaryLogicOperator.And
                    };
                    break;
                case ExpressionType.OrElse:
                    filter = new DBBinaryLogicFilter
                    {
                        Operator = DBBinaryLogicOperator.Or
                    };
                    break;
                case ExpressionType.LessThan:
                    filter = new DBBinaryComparisionFilter
                    {
                        Operator = DBBinaryComparisionOperator.LessThan
                    };
                    break;
                case ExpressionType.LessThanOrEqual:
                    filter = new DBBinaryComparisionFilter
                    {
                        Operator = DBBinaryComparisionOperator.LessEqual
                    };
                    break;
                case ExpressionType.GreaterThan:
                    filter = new DBBinaryComparisionFilter
                    {
                        Operator = DBBinaryComparisionOperator.GreaterThan
                    };
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    filter = new DBBinaryComparisionFilter
                    {
                        Operator = DBBinaryComparisionOperator.GreaterEqual
                    };
                    break;
                case ExpressionType.Equal:
                    filter = new DBBinaryComparisionFilter
                    {
                        Operator = DBBinaryComparisionOperator.Equal
                    };
                    break;
                case ExpressionType.NotEqual:
                    filter = new DBBinaryComparisionFilter
                    {
                        Operator = DBBinaryComparisionOperator.NotEqual
                    };
                    break;
                default:
                    throw new Exception("Expression expected");
            }
            if (filter is DBBinaryComparisionFilter)
            {
                ((DBBinaryComparisionFilter)filter).Left = this.TranslateParameter(expression.Left);
                ((DBBinaryComparisionFilter)filter).Right = this.TranslateParameter(expression.Right);
                this.FixBinaryComparisionFilter((DBBinaryComparisionFilter)filter);
            }
            else
            {
                ((DBBinaryLogicFilter)filter).Left = this.TranslateExpression(expression.Left);
                ((DBBinaryLogicFilter)filter).Right = this.TranslateExpression(expression.Right);
            }
            return filter;
        }

        protected IDBQueryFilter VisitMethodCall(MethodCallExpression expression)
        {
            //if (expression.Object.NodeType != ExpressionType.MemberAccess)
            //{
            //    throw new Exception("Member accessr expected for method call");
            //}
            DBBinaryComparisionFilter filter = new DBBinaryComparisionFilter();
            if (expression.Method.Name == "Contains")
            {
                filter.Operator = DBBinaryComparisionOperator.Contains;
            }
            else if (expression.Method.Name == "StartsWith")
            {
                filter.Operator = DBBinaryComparisionOperator.StartsWith;
            }
            else if (expression.Method.Name == "EndsWith")
            {
                filter.Operator = DBBinaryComparisionOperator.EndsWith;
            }
            else
            {
                throw new Exception("Unsupported method call");
            }
            filter.Left = this.TranslateParameter(expression.Object);
            filter.Right = this.TranslateParameter(expression.Arguments[0]);

            this.FixBinaryComparisionFilter(filter);           

            return filter;
        }

        protected IDBQueryParameter TranslateParameter(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Constant:
                    return this.VisitConstant((ConstantExpression)expression);
                case ExpressionType.MemberAccess:
                    return this.VisitMemberAccess((MemberExpression)expression);
                default:
                    throw new Exception("Parameter expected for parameter translate");
            }
        }

        protected IDBQueryParameter VisitConstant(ConstantExpression expression)
        {
            return new DBConstantParameter
            { 
                Type = expression.Type,
                Value = expression.Value
            };
        }

        protected IDBQueryParameter VisitMemberAccess(MemberExpression expression)
        {
            if (expression.Expression.NodeType != ExpressionType.Parameter)
            {
                throw new Exception("Parameter expected for member access");
            }
            ParameterExpression parameterExpression = (ParameterExpression)expression.Expression;
            if (parameterExpression.Name != _context.ParameterName)
            {
                throw new Exception("Unexpected paramName");
            }
            string memberName = expression.Member.Name;
            return new DBMemberAccessParameter
            {
                MemberName = memberName
            };
        }

        protected void FixBinaryComparisionFilter(DBBinaryComparisionFilter filter)
        {
            DBMemberAccessParameter param1 = null;
            DBConstantParameter param2 = null;
            if (filter.Left is DBMemberAccessParameter)
            {
                param1 = (DBMemberAccessParameter)filter.Left;
            }
            else if (filter.Right is DBMemberAccessParameter)
            {
                param1 = (DBMemberAccessParameter)filter.Right;
            }

            if (param1 is null)
            {
                return;
            }

            if (filter.Left is DBConstantParameter && !filter.Left.IsNullValue)
            {
                param2 = (DBConstantParameter)filter.Left;
            }
            else if (filter.Right is DBConstantParameter && !filter.Right.IsNullValue)
            {
                param2 = (DBConstantParameter)filter.Right;
            }
            if (param2 is null)
            {
                return;
            }
            string paraName;
            if (_context.ParameterUsed.ContainsKey(param1.MemberName))
            {
                ++_context.ParameterUsed[param1.MemberName];
                paraName = "@" + param1.MemberName + "_" + _context.ParameterUsed[param1.MemberName].ToString();
            }
            else
            {
                _context.ParameterUsed[param1.MemberName] = 1;
                paraName = "@" + param1.MemberName;
            }
            _statment.Parameters.Add(new DBFilterParameter
            {
                Name = paraName,
                Type = param2.Type,
                Value = param2.Value
            });
            param2.ParameterName = paraName;
        }
    }
}
