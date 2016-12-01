﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Science.Mathematics.Algebra
{
    /// <summary>
    /// Collects terms in a sum expression.
    /// </summary>
    internal sealed class CollectCoefficientsInSumSimplifier : ISimplifier<SumExpressionList>
    {
        private static readonly MultiplicationByOneSimplifier simplifier = new MultiplicationByOneSimplifier();

        public AlgebraExpression Simplify(SumExpressionList expression, CancellationToken cancellationToken)
        {
            var groups = expression.Terms
                .Where(t => t.GetConstantValue() == null)
                .Select(t => AsProduct(t))
                .GroupBy(t =>
                    ExpressionFactory.Product(
                        t.Terms
                            .Where(r => r.GetConstantValue() == null)
                            .ToImmutableList()
                    )
                )
                .Where(g => g.Key.Terms.Any()) // exclude constants
            ;

            var newTerms = expression.Terms
                .RemoveAll(e => groups.Any(g => g.Contains(AsProduct(e))))
                .InsertRange(0, 
                    groups.Select(g =>
                        simplifier.Simplify(
                            ExpressionFactory.Multiply(
                                g
                                    .SelectMany(p => // calculate coefficients
                                        p.Terms
                                            .Select(t => t.GetConstantValue())
                                            .Where(c => c != null)
                                            .DefaultIfEmpty(1)
                                    )
                                    .Sum(),
                                Normalize(g.Key)
                            ), cancellationToken
                        )
                    )
                );

            if (newTerms.Count == 1)
                return newTerms.Single();

            return expression.WithTerms(newTerms);
        }


        private static ProductExpressionList AsProduct(AlgebraExpression expression)
        {
            if (expression is ProductExpressionList)
                return expression as ProductExpressionList;

            return ExpressionFactory.Product(expression);
        }

        private static AlgebraExpression Normalize(ProductExpressionList expression)
        {
            if (expression.Terms.Count == 1)
                return expression.Terms.Single();

            return expression;
        }

        private sealed class ProductExpressionListComparer : IEqualityComparer<ProductExpressionList>
        {
            public bool Equals(ProductExpressionList x, ProductExpressionList y)
            {
                if (x.Terms.Count != y.Terms.Count)
                    return false;

                return x.Terms.All(t => y.Terms.Contains(t));
            }

            public int GetHashCode(ProductExpressionList obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}