﻿using ClosedXML.Excel;
using EntityDto;

namespace WorkSheetServices
{
    public static class WorkbookReportsService
    {
        public static byte[] GenerateReport(Dictionary<int, List<DispozitieLivrare>> dispozitii)
        {
            using (var ms = new MemoryStream())
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.AddWorksheet("Rezultate unite");
                    var values = dispozitii.Values.SelectMany(t => t).GroupBy(t => t.CodProdus.Substring(0, 2) + t.CodProdus.Substring(4, 1));

                    var ids = dispozitii.Keys.Order().ToArray();
                    Dictionary<int, int> rowCounter = new();

                    worksheet.Row(1).Style.Font.FontSize = 13;
                    worksheet.Range("A:D").Row(1).Merge();
                    worksheet.Cell(1, 1).Value = "Dispozitii Livrare:";
                    worksheet.Range("A:D").Row(2).Merge();
                    worksheet.Cell(2, 1).Value = string.Join("; ", ids);

                    worksheet.Row(3).Cells("1:3").Style.Border.SetBottomBorder(XLBorderStyleValues.Medium);
                    worksheet.Row(3).Style.Font.FontSize = 14;
                    worksheet.Cell(3, 1).Value = "Cod Produs";
                    worksheet.Cell(3, 2).Value = "Nume Produs";
                    worksheet.Cell(3, 3).Value = "Cnt";
                    worksheet.Cell(3, 4).Value = "Stoc";

                    var firstEmptyCol = 5;
                    var lastColCountIndex = firstEmptyCol + ids.Count() - 1;

                    for (var z = 0; z < ids.Count(); z++)
                    {
                        worksheet.Cell(3, firstEmptyCol + z).Value = ids[z];
                        worksheet.Cell(3, firstEmptyCol + ids.Count() + z + 3).Value = ids[z];
                        worksheet.Cell(3, firstEmptyCol + z).Style.Font.FontSize = 10;
                        var z_sheet = workbook.AddWorksheet(ids[z].ToString());
                        if (z % 2 == 0)
                            worksheet.Cell(3, 5 + z).Style.Fill.SetBackgroundColor(XLColor.LightGray);

                        z_sheet.Row(1).Cells("1:3").Style.Border.SetBottomBorder(XLBorderStyleValues.Medium);
                        z_sheet.Row(1).Style.Font.FontSize = 14;
                        z_sheet.Cell(1, 1).Value = "Cod Produs";
                        z_sheet.Cell(1, 2).Value = "Nume Produs";
                        z_sheet.Cell(1, 3).Value = "Cnt";
                        rowCounter[ids[z]] = 2;
                    }
                    workbook.FullCalculationOnLoad = true;
                    int i = 4;

                    values = values.OrderByDescending(t => t.Key).ToArray();

                    foreach (var grp in values)
                    {
                        int grp_i = i;
                        var ordered = grp.GroupBy(t => t.NumeProdus).OrderBy(t => t.Key).ToList();

                        foreach (var x in ordered)
                        {
                            if (i % 2 == 0)
                            {
                                worksheet.Row(i).Cells(string.Format("1:{0}", lastColCountIndex)).Style.Fill.SetBackgroundColor(XLColor.LightGray);
                            }
                            worksheet.Cell(i, 1).Value = x.First().CodProdus;
                            worksheet.Cell(i, 2).Value = x.First().NumeProdus;
                            worksheet.Cell(i, 3).FormulaR1C1 = string.Format("=SUM(R{0}C4:R{0}C{1})", i, lastColCountIndex);

                            for (var z = 0; z < ids.Count(); z++)
                            {
                                var item = x.Where(t => t.NumarIntern == ids[z]).FirstOrDefault();
                                if (workbook.TryGetWorksheet(ids[z].ToString(), out var z_sheet) && item != null)
                                {
                                    var row_count = rowCounter[ids[z]];
                                    z_sheet.Cell(row_count, 1).Value = item.CodProdus;
                                    z_sheet.Cell(row_count, 2).Value = item.NumeProdus;
                                    z_sheet.Cell(row_count, 3).Value = x.Where(t => t.NumarIntern == ids[z]).Sum(t => t.Cantitate);
                                    worksheet.Cell(i, firstEmptyCol + z).Value = x.Where(t => t.NumarIntern == ids[z]).Sum(t => t.Cantitate);
                                    worksheet.Cell(i, firstEmptyCol + z).Style.Font.FontSize = 10;
                                    rowCounter[ids[z]]++;
                                }

                            }
                            i++;
                        }

                        worksheet.Cell(i, 2).Value = string.Format("Total {0}:", GetDisplayName(grp.Key));
                        worksheet.Cell(i, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                        worksheet.Cell(i, 2).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                        worksheet.Cell(i, 3).FormulaR1C1 = string.Format("=SUM(R{0}C4:R{1}C{2})", grp_i, i - 1, lastColCountIndex);
                        worksheet.Cell(i, 3).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                        i++;
                    }

                    for (var z = 0; z < ids.Count(); z++)
                    {
                        if (workbook.TryGetWorksheet(ids[z].ToString(), out var z_sheet))
                        {
                            var range = worksheet.Range(4, firstEmptyCol + ids.Count() + z + 3, i - 2, firstEmptyCol + z + ids.Count() + 3);
                            range.FormulaR1C1 = string.Format("=IFERROR(@INDEX('{0}'!A2:C{1}, MATCH(RC1, '{0}'!A2:A{1}, 0),3), \"\")", ids[z].ToString(), rowCounter[ids[z]] + 1);
                            z_sheet.Columns("1:3").AdjustToContents();
                            z_sheet.Rows().AdjustToContents();
                        }
                    }
                    worksheet.Rows().AdjustToContents();
                    worksheet.Columns("1:" + lastColCountIndex).AdjustToContents();

                    worksheet.PageSetup.PrintAreas.Clear();
                    worksheet.PageSetup.PrintAreas.Add(1, 1, i, firstEmptyCol + ids.Count() - 1);
                    worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
                    worksheet.PageSetup.Margins.SetTop(1);
                    worksheet.PageSetup.Margins.SetLeft(0);
                    worksheet.PageSetup.Margins.SetRight(0);
                    worksheet.PageSetup.Margins.SetBottom(0);
                    worksheet.PageSetup.PagesWide = 1;
                    worksheet.PageSetup.PagesTall = 1;
                    worksheet.PageSetup.CenterHorizontally = true;

                    workbook.SaveAs(ms);
                    return ms.ToArray();
                }
            }

            string GetDisplayName(string s)
            {
                switch (s)
                {
                    case "MPN": return "Noptiera";
                    case "MPP": return "Pat";
                    case "MPS": return "Sifonier";
                    case "MPC": return "Comoda";
                    default: return s;
                }
            }
        }
    }
}