using ClosedXML.Excel;
using EntityDto;

namespace WorkSheetServices
{
    public static class WorkbookReportsService
    {
        public static byte[] GenerateReport(List<DispozitieLivrare> dispozitii,
            Func<DispozitieLivrare, string> keyResolver,
            Func<DispozitieLivrare, string> grouping1,
            Func<DispozitieLivrare, string> grouping2,
            string pageOrientation = "landscape")
        {
            using (var ms = new MemoryStream())
            {
                using (var workbook = new XLWorkbook())
                {
                    var ids = dispozitii.Select(keyResolver).Distinct().ToArray();

                    var firstEmptyCol = 4;
                    var lastColCountIndex = firstEmptyCol + ids.Count() - 1;

                    var worksheet = workbook.AddWorksheet("Rezultate unite");
                    worksheet.Style.Font.FontSize = 13;
                    var lines = dispozitii.GroupBy(keyResolver).OrderByDescending(t => t.Key).ToList();

                    Dictionary<string, int> rowCounter = new();

                    var lastCol = ((char)(lastColCountIndex + 64)).ToString();
                    worksheet.Row(1).Style.Font.FontSize = 14;
                    worksheet.Range(@$"A1:{lastCol}1").Merge();
                    worksheet.Cell(1, 1).Value = "Dispozitii Livrare: " + string.Join("; ", lines.Select(t => 
                        string.Format("{0} ( {1} )", t.Key, string.Join(", ", t.Select(t => t.NumarIntern).Distinct()))
                    ).ToArray());

                    int firstRow = 2;

                    worksheet.Row(firstRow).Cells("1:3").Style.Border.SetBottomBorder(XLBorderStyleValues.Medium);
                    worksheet.Row(firstRow).Style.Font.FontSize = 16;
                    worksheet.Cell(firstRow, 1).Value = "Cod Produs";
                    worksheet.Cell(firstRow, 2).Value = "Nume Produs";
                    worksheet.Cell(firstRow, 3).Value = "Q";

                    var extraInfo = dispozitii.GroupBy(keyResolver).ToDictionary(t => t.Key);
                    for (var z = 0; z < ids.Count(); z++)
                    {
                        worksheet.Cell(firstRow, firstEmptyCol + z).Value = ids[z];
                        worksheet.Cell(firstRow, firstEmptyCol + ids.Count() + z + 3).Value = ids[z];
                        worksheet.Cell(firstRow, firstEmptyCol + z).Style.Font.FontSize = 14;

                        var name = ids[z].ToString();
                        var z_sheet = workbook.AddWorksheet(name.Length > 30 ? name.Substring(0, 30) : name);
                        if (z % 2 == 0)
                            worksheet.Cell(firstRow, firstEmptyCol + z).Style.Fill.SetBackgroundColor(XLColor.LightGray);

                        z_sheet.Row(1).Cells("1:3").Style.Border.SetBottomBorder(XLBorderStyleValues.Medium);
                        z_sheet.Row(1).Style.Font.FontSize = 14;
                        z_sheet.Cell(1, 1).Value = "Cod Produs";
                        z_sheet.Cell(1, 2).Value = "Nume Produs";
                        z_sheet.Cell(1, 3).Value = "Q";
                        z_sheet.Cell(1, 4).Value = "Numar Comanda";
                        z_sheet.Range("A:D").Row(2).Merge();
                        z_sheet.Cell(2, 1).Value = string.Join(";", extraInfo[ids[z]].DistinctBy(t => t.NumarComanda).Select(t => t.NumarComanda));
                        rowCounter[ids[z]] = 3;
                    }
                    workbook.FullCalculationOnLoad = true;
                    int i = firstRow + 1;
                    XLColor[] skipcolors = [XLColor.Gainsboro, XLColor.Bisque, XLColor.LightGray, XLColor.MistyRose];
                    var cnt = skipcolors.Length;

                    var values = dispozitii.GroupBy(grouping1).OrderByDescending(t => t.Key).ToList();
                    foreach (var grp in values)
                    {
                        int grp_i = i;
                        var ordered = grp.GroupBy(grouping2).OrderBy(t => t.Key).ToList();
                        
                        var currentColorint = 0;
                        int currentColoredRow = 1;

                        foreach (var x in ordered)
                        {
                            var colorPrint = skipcolors[currentColorint % cnt];
                            if (currentColoredRow % 2 == 0)
                            {
                                worksheet.Row(i).Cells(string.Format("1:{0}", firstEmptyCol - 1)).Style.Fill.SetBackgroundColor(colorPrint);
                            }
                            worksheet.Cell(i, 1).Value = x.First().CodProdus;
                            worksheet.Cell(i, 2).Value = x.First().NumeProdus;
                            worksheet.Cell(i, 3).FormulaR1C1 = string.Format("=SUM(R{0}C4:R{0}C{1})", i, lastColCountIndex);

                            for (var z = 0; z < ids.Count(); z++)
                            {
                                var item = x.Where(t => keyResolver(t) == ids[z]).FirstOrDefault();
                                var sheetName = ids[z].Length > 30 ? ids[z].Substring(0, 30) : ids[z];
                                if (workbook.TryGetWorksheet(sheetName, out var z_sheet) && item != null)
                                {
                                    var row_count = rowCounter[ids[z]];
                                    z_sheet.Cell(row_count, 1).Value = item.CodProdus;
                                    z_sheet.Cell(row_count, 2).Value = item.NumeProdus;
                                    z_sheet.Cell(row_count, 3).Value = x.Where(t => keyResolver(t) == ids[z]).Sum(t => t.Cantitate);
                                    z_sheet.Cell(row_count, 4).Value = item.NumarComanda;
                                    worksheet.Cell(i, firstEmptyCol + z).Value = x.Where(t => keyResolver(t) == ids[z]).Sum(t => t.Cantitate);
                                    worksheet.Cell(i, firstEmptyCol + z).Style.Font.FontSize = 14;
                                    worksheet.Cell(i, firstEmptyCol + z).Style.Border.SetRightBorder(XLBorderStyleValues.Thin);
                                    worksheet.Cell(i, firstEmptyCol + z).Style.Border.SetRightBorderColor(XLColor.Black);
                                    if (currentColoredRow % 2 == 0)
                                    {
                                        worksheet.Cell(i, firstEmptyCol + z).Style.Fill.SetBackgroundColor(colorPrint);
                                    }
                                    rowCounter[ids[z]]++;
                                }
                            }
                            if (currentColoredRow++ % 2 == 0)
                            {
                                currentColorint++;
                            }
                            i++;
                        }

                        if (ordered.Count() > 1)
                        {
                            worksheet.Cell(i, 2).Value = string.Format("Total {0}:", GetDisplayName(grp.Key));
                            worksheet.Cell(i, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                            worksheet.Cell(i, 2).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                            worksheet.Cell(i, 3).FormulaR1C1 = string.Format("=SUM(R{0}C4:R{1}C{2})", grp_i, i - 1, lastColCountIndex);
                            worksheet.Cell(i, 3).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                            for (var z = 0; z < ids.Count(); z++)
                            {
                                worksheet.Cell(i, firstEmptyCol + z).FormulaR1C1 = string.Format("=SUM(R{0}C{1}:R{2}C{3})", grp_i, firstEmptyCol + z, i - 1, firstEmptyCol + z);
                            }

                            //worksheet.Row(i).Cells($@"2:{firstEmptyCol + ids.Count() - 1}").Style.Fill.SetBackgroundColor(XLColor.PaleRobinEggBlue);
                            worksheet.Row(i).Cells($@"{firstEmptyCol}:{firstEmptyCol + ids.Count() - 1}").Style.Border.SetOutsideBorder(XLBorderStyleValues.Dashed);
                            i++;
                        }
                        else
                        {
                            worksheet.Row(i - 1).Cells(@$"1:{lastColCountIndex}").Style.Fill.SetBackgroundColor(XLColor.LightCyan);
                        }
                    }

                    for (var z = 0; z < ids.Count(); z++)
                    {
                        if (workbook.TryGetWorksheet(ids[z].ToString(), out var z_sheet))
                        {
                            var range = worksheet.Range(firstRow + 1, firstEmptyCol + ids.Count() + z + 3, i, firstEmptyCol + z + ids.Count() + 3);
                            range.FormulaR1C1 = string.Format("=IFERROR(@INDEX('{0}'!A2:C{1}, MATCH(RC1, '{0}'!A2:A{1}, 0),3), \"\")", ids[z].ToString(), rowCounter[ids[z]] + 1);
                            z_sheet.Columns("1:4").AdjustToContents();
                            z_sheet.Rows().AdjustToContents();
                        }
                    }

                    var table = worksheet.Range(firstRow, 1, i - 1, 3).CreateTable();
                    table.Theme = XLTableTheme.None;

                    worksheet.Rows().AdjustToContents();
                    worksheet.Columns("1:" + lastColCountIndex).AdjustToContents();

                    worksheet.PageSetup.PrintAreas.Clear();
                    worksheet.PageSetup.PrintAreas.Add(1, 1, i + 1, lastColCountIndex);
                    worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;// pageOrientation == "landscape" ? XLPageOrientation.Landscape : XLPageOrientation.Portrait;
                    worksheet.PageSetup.Margins.SetTop(1);
                    worksheet.PageSetup.Margins.SetLeft(0);
                    worksheet.PageSetup.Margins.SetRight(0);
                    worksheet.PageSetup.Margins.SetBottom(0);
                    worksheet.PageSetup.PagesWide = 1;
                    worksheet.PageSetup.CenterHorizontally = true;

                    worksheet.PageSetup.Header.Center.AddText(XLHFPredefinedText.PageNumber, XLHFOccurrence.AllPages);
                    worksheet.PageSetup.Header.Center.AddText(" / ", XLHFOccurrence.AllPages);
                    worksheet.PageSetup.Header.Center.AddText(XLHFPredefinedText.NumberOfPages, XLHFOccurrence.AllPages);
                    worksheet.PageSetup.Header.Left.AddText("Artkubika", XLHFOccurrence.AllPages);
                    worksheet.PageSetup.Header.Right.AddText(XLHFPredefinedText.Date, XLHFOccurrence.AllPages);

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
