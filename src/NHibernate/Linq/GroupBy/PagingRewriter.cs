using System.Linq;
using System.Linq.Expressions;
using NHibernate.Linq.Visitors;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.ResultOperators;

namespace NHibernate.Linq.GroupBy
{
	internal static class PagingRewriter
	{
		private static readonly System.Type[] PagingResultOperators = new[]
																		  {
																			  typeof (SkipResultOperator),
																			  typeof (TakeResultOperator),
																		  };

		public static void ReWrite(QueryModel queryModel)
		{
			var subQueryExpression = queryModel.MainFromClause.FromExpression as SubQueryExpression;

			if (subQueryExpression != null &&
				subQueryExpression.QueryModel.ResultOperators.All(x => PagingResultOperators.Contains(x.GetType())))
			{
				FlattenSubQuery(subQueryExpression, queryModel);
			}
		}

		private static void FlattenSubQuery(SubQueryExpression subQueryExpression, QueryModel queryModel)
		{
			// we can not flatten subquery if outer query has body clauses.
			var subQueryModel = subQueryExpression.QueryModel;
			var subQueryMainFromClause = subQueryModel.MainFromClause;
			if (queryModel.BodyClauses.Count == 0)
			{
				foreach (var resultOperator in subQueryModel.ResultOperators)
					queryModel.ResultOperators.Add(resultOperator);

				foreach (var bodyClause in subQueryModel.BodyClauses)
					queryModel.BodyClauses.Add(bodyClause);

				var visitor1 = new PagingRewriterSelectClauseVisitor(queryModel.MainFromClause);
				queryModel.SelectClause.TransformExpressions(visitor1.Swap);
			}
			else if (queryModel.ResultOperators.Count == 1 && queryModel.ResultOperators[0] is AnyResultOperator &&
			         queryModel.BodyClauses.Count == 1 && queryModel.BodyClauses[0] is WhereClause whereClause &&
			         whereClause.Predicate is BinaryExpression whereClausePredicate &&
			         new[] {whereClausePredicate.Left, whereClausePredicate.Right}.Count(x => x.NodeType == ExpressionType.MemberAccess) == 1)
			{
				var joinOnBodyClause = whereClausePredicate.Right.NodeType == ExpressionType.MemberAccess
					? whereClausePredicate.Right
					: whereClausePredicate.Left;
				var cro = new ContainsResultOperator(joinOnBodyClause);

				queryModel.BodyClauses.Clear();
				foreach (var orderByClause in subQueryModel.BodyClauses)
				{
					queryModel.BodyClauses.Add(orderByClause);
				}

				queryModel.ResultOperators.Clear();
				foreach (var resultOperator in subQueryModel.ResultOperators)
				{
					queryModel.ResultOperators.Add(resultOperator);
				}

				queryModel.ResultOperators.Add(cro);
				queryModel.ResultTypeOverride = typeof(bool);
			}
			else
			{
				var cro = new ContainsResultOperator(new QuerySourceReferenceExpression(subQueryMainFromClause));

				var newSubQueryModel = subQueryModel.Clone();
				newSubQueryModel.ResultOperators.Add(cro);
				newSubQueryModel.ResultTypeOverride = typeof (bool);

				var where = new WhereClause(new SubQueryExpression(newSubQueryModel));
				queryModel.BodyClauses.Add(where);

				if (!queryModel.BodyClauses.OfType<OrderByClause>().Any() && 
					!(queryModel.ResultOperators.Count == 1 && queryModel.ResultOperators.All(x => x is AnyResultOperator || x is ContainsResultOperator || x is AllResultOperator)))
				{
					var orderByClauses = subQueryModel.BodyClauses.OfType<OrderByClause>();
					foreach (var orderByClause in orderByClauses)
						queryModel.BodyClauses.Add(orderByClause);
				}
			}

			// Point all query source references to the outer from clause
			var visitor2 = new SwapQuerySourceVisitor(queryModel.MainFromClause, subQueryMainFromClause);
			queryModel.TransformExpressions(visitor2.Swap);

			// Replace the outer query source
			queryModel.MainFromClause = subQueryMainFromClause;
		}
	}
}
