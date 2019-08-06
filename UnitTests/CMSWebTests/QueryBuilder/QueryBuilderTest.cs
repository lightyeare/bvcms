﻿using Xunit;
using Shouldly;
using System.Collections.Generic;
using System.Collections;
using UtilityExtensions;

namespace CmsData.QueryBuilder
{
    public class PythonScript : IEnumerable<object[]>
    {
        private readonly List<object[]> _data = new List<object[]>
        {
            new object[] { "#Roles=Finance\r\n\r\ntemplate = \"\"\"\r\n;WITH givingunits AS (\r\n    SELECT p.PeopleId FROM dbo.People p JOIN dbo.TagPerson tp ON tp.PeopleId = p.PeopleId AND tp.Id = 1\r\n    UNION\r\n    SELECT p.SpouseId FROM dbo.People p JOIN dbo.TagPerson tp ON tp.PeopleId = p.PeopleId AND tp.Id = @BlueToolbarTagId\r\n    WHERE ISNULL(p.ContributionOptionsId, IIF(p.MaritalStatusId = 2, 2, 1)) = 2\r\n)\r\nSELECT  CreditGiverId,\r\n        SpouseId,\r\n        Amount,\r\n        DATEPART(YEAR, Date) Y,\r\n        c.PeopleId,\r\n        Date,\r\n        c.FundId\r\n        INTO #t\r\n    FROM dbo.Contributions2('1/1/{0}','12/31/{2}',0,0,NULL,NULL) c\r\n    WHERE EXISTS(SELECT NULL FROM givingunits WHERE PeopleId IN (c.CreditGiverId, c.CreditGiverId2))\r\n    AND c.FundId IN (SELECT [FundId] FROM [dbo].[ContributionFund] WHERE [FundName] = 'Mount Pisgah Church')\r\n    AND Amount > 0\r\n\r\n;WITH giving AS (\r\n    SELECT\r\n        CreditGiverId, SpouseId\r\n        , ISNULL((SELECT SUM(Amount)\r\n            FROM #t\r\n            WHERE CreditGiverId = tt.CreditGiverId\r\n            AND Y = {0}), 0) Tot{0}\r\n        , ISNULL((SELECT SUM(Amount)\r\n            FROM #t\r\n            WHERE CreditGiverId = tt.CreditGiverId\r\n            AND Y = {1}), 0) Tot{1}\r\n        , ISNULL((SELECT SUM(Amount)\r\n            FROM #t\r\n            WHERE CreditGiverId = tt.CreditGiverId\r\n            AND Y = {2}), 0) Tot{2}\r\n    FROM #t tt\r\n    GROUP BY tt.CreditGiverId, tt.SpouseId\r\n)\r\nSELECT\r\n    p.PeopleId,\r\n    Head = p.Name2,\r\n    Spouse = sp.PreferredName,\r\n    g.Tot{0},\r\n    g.Tot{1},\r\n    g.Tot{2}\r\nFROM giving g\r\nJOIN dbo.People p ON p.PeopleId = g.CreditGiverId\r\nLEFT JOIN dbo.People sp ON sp.PeopleId = g.SpouseId\r\nORDER BY p.Name2\r\n\r\nDROP TABLE #t\r\n\"\"\"\r\nyear = model.DateTime.Year - 3\r\nsql = template.format(year, year + 1, year + 2)\r\n\r\nprint model.SqlGrid(sql)" },
            new object[] { "#Roles=Finance\r\n\r\ntemplate = \"\"\"\r\n;WITH givingunits AS (\r\n    SELECT p.PeopleId FROM dbo.People p JOIN dbo.TagPerson tp ON tp.PeopleId = p.PeopleId AND tp.Id = 1\r\n    UNION\r\n    SELECT p.SpouseId FROM dbo.People p JOIN dbo.TagPerson tp ON tp.PeopleId = p.PeopleId AND tp.Id = @BlueToolbarTagId\r\n    WHERE ISNULL(p.ContributionOptionsId, IIF(p.MaritalStatusId = 2, 2, 1)) = 2\r\n)\r\nSELECT  CreditGiverId,\r\n        SpouseId,\r\n        Amount,\r\n        DATEPART(YEAR, Date) Y,\r\n        c.PeopleId,\r\n        Date,\r\n        c.FundId\r\n        INTO #t\r\n    FROM dbo.Contributions2('1/1/{0}','12/31/{4}',0,0,NULL,NULL) c\r\n    WHERE EXISTS(SELECT NULL FROM givingunits WHERE PeopleId IN (c.CreditGiverId, c.CreditGiverId2))\r\n    AND c.FundId IN (SELECT [FundId] FROM [dbo].[ContributionFund] WHERE [FundName] = 'Mount Pisgah Church')\r\n    AND Amount > 0\r\n\r\n;WITH giving AS (\r\n    SELECT\r\n        CreditGiverId, SpouseId\r\n        , ISNULL((SELECT SUM(Amount)\r\n            FROM #t\r\n            WHERE CreditGiverId = tt.CreditGiverId\r\n            AND Y = {0}), 0) Tot{0}\r\n        , ISNULL((SELECT SUM(Amount)\r\n            FROM #t\r\n            WHERE CreditGiverId = tt.CreditGiverId\r\n            AND Y = {1}), 0) Tot{1}\r\n        , ISNULL((SELECT SUM(Amount)\r\n            FROM #t\r\n            WHERE CreditGiverId = tt.CreditGiverId\r\n            AND Y = {2}), 0) Tot{2}\r\n        , ISNULL((SELECT SUM(Amount)\r\n            FROM #t\r\n            WHERE CreditGiverId = tt.CreditGiverId\r\n            AND Y = {3}), 0) Tot{3}\r\n        , ISNULL((SELECT SUM(Amount)\r\n            FROM #t\r\n            WHERE CreditGiverId = tt.CreditGiverId\r\n            AND Y = {4}), 0) Tot{4}\r\n    FROM #t tt\r\n    GROUP BY tt.CreditGiverId, tt.SpouseId\r\n)\r\nSELECT\r\n    p.PeopleId,\r\n    Head = p.Name2,\r\n    Spouse = sp.PreferredName,\r\n    g.Tot{0},\r\n    g.Tot{1},\r\n    g.Tot{2},\r\n    g.Tot{3},\r\n    g.Tot{4}\r\nFROM giving g\r\nJOIN dbo.People p ON p.PeopleId = g.CreditGiverId\r\nLEFT JOIN dbo.People sp ON sp.PeopleId = g.SpouseId\r\nORDER BY p.Name2\r\n\r\nDROP TABLE #t\r\n\"\"\"\r\nyear = model.DateTime.Year - 5\r\nsql = template.format(year, year + 1, year + 2, year + 3, year + 4)\r\n#print '<pre>', sql, '</pre>'\r\nprint model.SqlGrid(sql)" }
        };

        public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Collection("Database collection")]
    public class QueryBuilderTest
    {
        [Theory]
        [ClassData(typeof(PythonScript))]
        public void ShouldRunPythonScriptFromString(string script)
        {
            var pe = new PythonModel(CMSDataContext.Create(Util.Host));

            var result = pe.RunScript(script);
            result.ShouldNotBeNull();
        }
    }
}
