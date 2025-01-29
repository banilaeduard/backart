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
                    var results = dispozitii.GroupBy(keyResolver).OrderByDescending(x => x.DistinctBy(grouping2).Count());
                    var ids = results.Select(x => x.Key).Distinct().ToArray();

                    var firstEmptyCol = 3;
                    var lastColCountIndex = firstEmptyCol + ids.Count() - 1;

                    var worksheet = workbook.AddWorksheet("Rezultate unite");
                    worksheet.Style.Font.FontSize = 15;
                    var lines = results.ToList();

                    var lastCol = lastColCountIndex < 22 ? ((char)(lastColCountIndex + 64)).ToString() : "Z";
                    worksheet.Range(@$"A1:{lastCol}1").Merge();
                    worksheet.Cell(1, 1).Value = "Dispozitii Livrare: " + string.Join("; ", lines.Select(t => 
                        string.Format("{0} ( {1} )", t.Key, string.Join(", ", t.Select(t => t.NumarIntern).Distinct()))
                    ).ToArray());

                    int firstRow = 2;

                    worksheet.Row(firstRow).Cells("1:2").Style.Border.SetBottomBorder(XLBorderStyleValues.Medium);
                    worksheet.Cell(firstRow, 1).Value = "Nume Produs";
                    worksheet.Cell(firstRow, 2).Value = "Q";

                    var extraInfo = results.ToDictionary(t => t.Key);
                    for (var z = 0; z < ids.Count(); z++)
                    {
                        worksheet.Cell(firstRow, firstEmptyCol + z).Value = ids[z];

                        if (z % 2 == 0)
                            worksheet.Cell(firstRow, firstEmptyCol + z).Style.Fill.SetBackgroundColor(XLColor.LightGray);
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
                            worksheet.Cell(i, 1).Value = x.First().NumeProdus;
                            worksheet.Cell(i, 2).FormulaR1C1 = string.Format("=SUM(R{0}C3:R{0}C{1})", i, lastColCountIndex);

                            for (var z = 0; z < ids.Count(); z++)
                            {
                                var items = x.Where(t => keyResolver(t) == ids[z]).ToList();

                                if (items.Any())
                                {
                                    worksheet.Cell(i, firstEmptyCol + z).Value = items.Sum(t => t.Cantitate);
                                    worksheet.Cell(i, firstEmptyCol + z).Style.Border.SetRightBorder(XLBorderStyleValues.Thin);
                                    worksheet.Cell(i, firstEmptyCol + z).Style.Border.SetRightBorderColor(XLColor.Black);
                                    if (currentColoredRow % 2 == 0)
                                    {
                                        worksheet.Cell(i, firstEmptyCol + z).Style.Fill.SetBackgroundColor(colorPrint);
                                    }
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
                            worksheet.Cell(i, 1).Value = string.Format("Total {0}:", GetDisplayName(grp.Key));
                            worksheet.Cell(i, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                            worksheet.Cell(i, 1).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                            worksheet.Cell(i, 2).FormulaR1C1 = string.Format("=SUM(R{0}C3:R{1}C{2})", grp_i, i - 1, lastColCountIndex);
                            worksheet.Cell(i, 2).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

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

                    var table = worksheet.Range(firstRow, 1, i - 1, 2).CreateTable();
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
