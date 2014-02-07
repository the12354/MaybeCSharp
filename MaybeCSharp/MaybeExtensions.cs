using System;
using System.Collections.Generic;
using System.Linq.Expressions;
// ReSharper disable CompareNonConstrainedGenericWithNull
namespace MaybeCSharp
{
    public static class MaybeExtensions
    {
        private static Dictionary<string, Delegate> _cachedDelegates = new Dictionary<string, Delegate>();
        private static Func<TInput, Maybe<TOutput>> GetCachedDelegate<TInput, TOutput>(Expression expression)
        {
            var stringRepresentation = expression.ToString();
            Delegate cached;
            if (_cachedDelegates.TryGetValue(stringRepresentation, out cached))
            {
                var cachedFunc = cached as Func<TInput, Maybe<TOutput>>;
                if (cachedFunc == null)
                    throw new NotSupportedException("Cached delegate doesn't have the correct types <" + typeof(TInput) + "," + typeof(TOutput) + ">");
                return cachedFunc;
            }
            return null;
        }
        private static void AddDelegateToCache<TInput, TOutput>(Expression expression, Func<TInput, Maybe<TOutput>> func)
        {
            var stringRepresentation = expression.ToString();
            if (_cachedDelegates.ContainsKey(stringRepresentation))
                throw new NotSupportedException("Delegate cache already contains expression " + expression);
            _cachedDelegates[stringRepresentation] = func;
        }
        private static IEnumerable<Expression> GetCheckedExpressions<TInput, TOutput>(
            Expression<Func<TInput, TOutput>> expression)
        {
            var stack = new Stack<Expression>();
            var currentExpression = expression.Body;
            while (currentExpression != null)
            {
                stack.Push(currentExpression);
                var member = currentExpression as MemberExpression;
                var call = currentExpression as MethodCallExpression;
                var parameter = currentExpression as ParameterExpression;
                if (member != null)
                {
                    currentExpression = member.Expression;
                }
                else if (call != null)
                {
                    currentExpression = call.Object;
                }
                else if (parameter != null)
                {
                    break;
                }
                else 
                {
                    throw new NotSupportedException("SubExpression Type not supported: " + currentExpression.GetType());
                }
            }
            return stack;
        }
        private static Expression ChangeExpressionSource(Expression expression, ParameterExpression newSource)
        {
            var member = expression as MemberExpression;
            if (member != null)
                return Expression.MakeMemberAccess(newSource, member.Member);

            var call = expression as MethodCallExpression;
            if (call != null)
                return Expression.Call(newSource, call.Method, call.Arguments);
            throw new NotSupportedException("Can't change source of expression: " + expression);
        }
        public static Maybe<TOutput> Maybe<TInput, TOutput>(this TInput input, Expression<Func<TInput, TOutput>> expression)
        {

            if (input == null)
                return new Maybe<TOutput>();
            if(expression == null)
                throw new ArgumentNullException("expression");

            var cachedFunc = GetCachedDelegate<TInput, TOutput>(expression);
            if (cachedFunc != null)
                return cachedFunc(input);

            var expressions = GetCheckedExpressions(expression);

            var lambdaBlock = new List<Expression>();
            var returnTarget = Expression.Label(typeof(Maybe<TOutput>), "ReturnNull");
            var returnNull = Expression.Return(returnTarget, Expression.Constant(new Maybe<TOutput>()));

            var variables = new Stack<ParameterExpression>();
            foreach (var exp in expressions)
            {
                var localExp = exp;

                var variable = Expression.Variable(exp.Type);
                if (variables.Count > 0) //Only change the expression source AFTER the first variable assignment
                    localExp = ChangeExpressionSource(exp, variables.Peek());
                variables.Push(variable);

                var assignment = Expression.Assign(variable, localExp);
                lambdaBlock.Add(assignment);

                if (!variable.Type.IsValueType) //If our variable is a reference type check for null
                {
                    lambdaBlock.Add(
                        Expression.IfThen(
                            Expression.Equal(variable, Expression.Constant(null)),
                            returnNull));
                }
            }
            var constructor = typeof(Maybe<TOutput>).GetConstructor(new[] { variables.Peek().Type });
            if (constructor == null)
                throw new NotSupportedException("Maybe class doesn't have the Maybe(" + variables.Peek().Type + ") constructor");

            lambdaBlock.Add(Expression.Label(returnTarget,
                Expression.New(constructor, variables.Peek()))); //The last expression determines the return type

            var block = Expression.Block(variables, lambdaBlock);
            var lambda = Expression.Lambda<Func<TInput, Maybe<TOutput>>>(block, expression.Parameters);
            var compiled = lambda.Compile();
            AddDelegateToCache(expression, compiled);
            return compiled(input);
        }
    }
}
// ReSharper enable CompareNonConstrainedGenericWithNull