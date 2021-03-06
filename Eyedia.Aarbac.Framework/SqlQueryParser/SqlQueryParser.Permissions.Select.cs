#region Copyright Notice
/* Copyright (c) 2017, Deb'jyoti Das - debjyoti@debjyoti.com
 All rights reserved.

 Redistribution and use in source and binary forms, with or without
 modification, are not permitted.Neither the name of the 
 'Deb'jyoti Das' nor the names of its contributors may be used 
 to endorse or promote products derived from this software without 
 specific prior written permission.

 THIS SOFTWARE IS PROVIDED BY Deb'jyoti Das 'AS IS' AND ANY
 EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 DISCLAIMED. IN NO EVENT SHALL Synechron Holdings Inc BE LIABLE FOR ANY
 DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
#region Developer Information
/*
Author  - Debjyoti Das (debjyoti@debjyoti.com)
Created - 11/12/2017 3:31:31 PM
Description  - 
Modified By - 
Description  - 
*/
#endregion Developer Information

#endregion Copyright Notice
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Eyedia.Aarbac.Framework
{
    public partial class SqlQueryParser
    {       
        public void ApplyPermissionSelect()
        {
            var tables = Columns.GroupBy(c => c.Table.Name).Select(grp => grp.ToList()).ToList();
            foreach (var allColumnnsInATable in tables)
            {
                if (allColumnnsInATable.Count > 0)
                {
                    //RbacTable rbacTable = TablesReferred.Find(allColumnnsInATable[0].Table.Name);
                    //if (rbacTable == null)
                    //    throw new Exception("Could not find table name in referred tables!");
                    if (allColumnnsInATable[0].Table.AllowedOperations.HasFlag(RbacDBOperations.Read))
                    {
                        foreach (RbacSelectColumn column in allColumnnsInATable)
                        {
                            RbacColumn rbacColumn = allColumnnsInATable[0].Table.FindColumn(column.Name);

                            if (rbacColumn == null)
                                RbacException.Raise(
                                    string.Format("Role '{0}' belongs to '{1}' is not in sync with database. The column '{2}' of table '{3}' was not found in the role meta data",
                                    this.Context.User.UserName, this.Context.User.Role.Name, column.Name, column.Table.Name));
                            
                            if (!rbacColumn.AllowedOperations.HasFlag(RbacDBOperations.Read))
                                RemoveColumnFromSelect(column);
                        }
                    }
                    else
                    {
                        //user do not have access to this table
                        RemoveColumnFromSelect(allColumnnsInATable);
                    }
                }
            }

            IsPermissionApplied = true;
        }

        private void RemoveColumnFromSelect(List<RbacSelectColumn> columns)
        {
            foreach(RbacSelectColumn column in columns)
            {
                RemoveColumnFromSelect(column);
            }
        }
        private void RemoveColumnFromSelect(RbacSelectColumn column)
        {
            //if (column.TableColumnName == "SSN")
            //    Debugger.Break();
            SelectColumnRemover selectColumnRemover = new SelectColumnRemover(ParsedQuery, column);
            ParsedQuery = selectColumnRemover.Remove();

            if ((IsSilent == false)
            && selectColumnRemover.IsZeroSelectColumn)
                RbacException.Raise("The query returned 0(zero) column!");

        }
    }
}

