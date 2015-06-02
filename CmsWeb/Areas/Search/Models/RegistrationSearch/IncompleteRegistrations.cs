using System;
using System.Collections.Generic;
using System.Diagnostics.PerformanceData;
using System.Linq;
using CmsData;
using CmsData.View;
using CmsWeb.Models;

namespace CmsWeb.Areas.Search.Models
{
    public class IncompleteRegistrations : PagedTableModel<RecentIncompleteRegistrations2, RecentIncompleteRegistrations2>
    {
        public int? days { get; set; }
        public string oids { get; set; }

        public IncompleteRegistrations()
            : this(null, null)
        {
        }

        public IncompleteRegistrations(OrgSearchModel orgsearch, int? days)
            : base("Date", "desc", true)
        {
            if (orgsearch != null)
            {
                var q = orgsearch.FetchOrgs();
                oids = string.Join(",", q.OrderBy(mm => mm.OrganizationName).Select(mm => mm.OrganizationId));
            }
            this.days = days;
            pagesize = 0;
            ShowPageSize = false;
        }

        private int? count;
        public override int Count()
        {
            if (count == null)
                count = GetList().Count();
            return count.Value;
        }

        private IQueryable<RecentIncompleteRegistrations2> GetList()
        {
            if (list != null)
                return list;
            var q = DbUtil.Db.RecentIncompleteRegistrations2(oids, days);
            switch (SortExpression)
            {
                case "Date":
                    q = from r in q
                        orderby r.Stamp
                        select r;
                    break;
                case "Person":
                    q = from r in q
                        orderby r.Name
                        select r;
                    break;
                case "Organization":
                    q = from r in q
                        orderby r.OrgName, r.Name
                        select r;
                    break;
                case "Date desc":
                    q = from r in q
                        orderby r.Stamp descending
                        select r;
                    break;
                case "Person desc":
                    q = from r in q
                        orderby r.Name descending
                        select r;
                    break;
                case "Organization desc":
                    q = from r in q
                        orderby r.OrgName descending, r.Name
                        select r;
                    break;
            }
            return list = q.ToList().AsQueryable();
        }

        public override IQueryable<RecentIncompleteRegistrations2> DefineModelList()
        {
            return GetList();
        }

        public override IQueryable<RecentIncompleteRegistrations2> DefineModelSort(IQueryable<RecentIncompleteRegistrations2> q)
        {
            return q;
        }

        public override IEnumerable<RecentIncompleteRegistrations2> DefineViewList(IQueryable<RecentIncompleteRegistrations2> q)
        {
            return q;
        }
    }
}