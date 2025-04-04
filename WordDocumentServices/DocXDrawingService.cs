using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace WordDocument.Services
{
    public static class DocXDrawingService
    {
        public static Drawing CreateImageElement(string relationshipId, string name, long widthPx, long heightPx)
        {
            const long EMU_PER_PIXEL = 9525;
            long widthEmu = widthPx * EMU_PER_PIXEL;
            long heightEmu = heightPx * EMU_PER_PIXEL;

            return new Drawing(
                new DW.Anchor(
                    new DW.SimplePosition() { X = 0L, Y = 0L },
                    new DW.HorizontalPosition(
                        new DW.PositionOffset("0")
                    )
                    { RelativeFrom = DW.HorizontalRelativePositionValues.Character },
                    new DW.VerticalPosition(
                        new DW.PositionOffset("0")
                    )
                    { RelativeFrom = DW.VerticalRelativePositionValues.Paragraph },
                    new DW.Extent() { Cx = widthEmu, Cy = heightEmu },
                    new DW.EffectExtent()
                    {
                        LeftEdge = 0L,
                        TopEdge = 0L,
                        RightEdge = 0L,
                        BottomEdge = 0L
                    },
                    new DW.WrapNone(),
                    new DW.DocProperties()
                    {
                        Id = (UInt32Value)1U,
                        Name = name
                    },
                    new DW.NonVisualGraphicFrameDrawingProperties(
                        new A.GraphicFrameLocks() { NoChangeAspect = true }),
                    new A.Graphic(
                        new A.GraphicData(
                            new PIC.Picture(
                                new PIC.NonVisualPictureProperties(
                                    new PIC.NonVisualDrawingProperties()
                                    {
                                        Id = (UInt32Value)0U,
                                        Name = name
                                    },
                                    new PIC.NonVisualPictureDrawingProperties()
                                ),
                                new PIC.BlipFill(
                                    new A.Blip()
                                    {
                                        Embed = relationshipId,
                                        CompressionState = A.BlipCompressionValues.Print,
                                    },
                                    new A.Stretch(new A.FillRectangle())
                                ),
                                new PIC.ShapeProperties(
                                    new A.Transform2D(
                                        new A.Offset() { X = 0, Y = 0 },
                                        new A.Extents() { Cx = widthEmu, Cy = heightEmu }
                                    ),
                                    new A.PresetGeometry(new A.AdjustValueList())
                                    { Preset = A.ShapeTypeValues.Rectangle }
                                )
                            )
                        )
                        { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
                    )
                )
                {
                    DistanceFromTop = 0U,
                    DistanceFromBottom = 0U,
                    DistanceFromLeft = 0U,
                    DistanceFromRight = UInt32Value.FromUInt32((uint)(widthPx)),
                    SimplePos = false,
                    RelativeHeight = (UInt32Value)251658240U,
                    BehindDoc = true,                // <<< this puts the image behind the text
                    Locked = false,
                    LayoutInCell = true,
                    AllowOverlap = true
                }
            );
        }

        public static Drawing CreateFloatingImage(string relationshipId, string name, long widthPx, long heightPx)
        {
            const long EMU_PER_PIXEL = 9525;
            long widthEmu = widthPx * EMU_PER_PIXEL;
            long heightEmu = heightPx * EMU_PER_PIXEL;

            long pageWidthEmu = 12240 * EMU_PER_PIXEL;  // Approx 8.5 inches in EMUs
            long rightMarginEmu = 1440 * EMU_PER_PIXEL; // 1-inch margin in EMUs

            long maxRightPositionEmu = pageWidthEmu - widthEmu - rightMarginEmu;

            return new Drawing(
                new DW.Anchor(
                    new DW.SimplePosition() { X = 0L, Y = 0L },
                    new DW.HorizontalPosition(
                        new DW.PositionOffset(@$"-{(int)(1.5 * widthEmu)}")  // Adjust to fit inside document
                    )
                    { RelativeFrom = DW.HorizontalRelativePositionValues.RightMargin },  // Align to margin, not beyond

                    new DW.VerticalPosition(
                        new DW.PositionOffset("0")
                    )
                    { RelativeFrom = DW.VerticalRelativePositionValues.Paragraph }, // Keep relative to text

                    new DW.Extent() { Cx = widthEmu, Cy = heightEmu },
                    new DW.EffectExtent() { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                    new DW.WrapNone(),  // No text wrap
                    new DW.DocProperties() { Id = (UInt32Value)1U, Name = name },
                    new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks() { NoChangeAspect = true }),

                    new A.Graphic(
                        new A.GraphicData(
                            new PIC.Picture(
                                new PIC.NonVisualPictureProperties(
                                    new PIC.NonVisualDrawingProperties()
                                    {
                                        Id = (UInt32Value)0U,
                                        Name = name
                                    },
                                    new PIC.NonVisualPictureDrawingProperties()
                                ),
                                new PIC.BlipFill(
                                    new A.Blip()
                                    {
                                        Embed = relationshipId,
                                        CompressionState = A.BlipCompressionValues.Print
                                    },
                                    new A.Stretch(new A.FillRectangle())
                                ),
                                new PIC.ShapeProperties(
                                    new A.Transform2D(
                                        new A.Offset() { X = 0, Y = 0 },
                                        new A.Extents() { Cx = widthEmu, Cy = heightEmu }
                                    ),
                                    new A.PresetGeometry(new A.AdjustValueList())
                                    { Preset = A.ShapeTypeValues.Rectangle }
                                )
                            )
                        )
                        { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
                    )
                )
                {
                    DistanceFromTop = 0U,
                    DistanceFromBottom = 0U,
                    DistanceFromLeft = 0U,
                    DistanceFromRight = 0U,
                    SimplePos = false,
                    RelativeHeight = (UInt32Value)251658240U,
                    BehindDoc = true,  // Keep behind text
                    Locked = false,
                    LayoutInCell = true,
                    AllowOverlap = true
                }
            );
        }

    }
}