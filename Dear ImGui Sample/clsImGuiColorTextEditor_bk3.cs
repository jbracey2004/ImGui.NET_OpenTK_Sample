using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dear_ImGui_Sample;
using ImGuiNET;
using System.Numerics;
namespace WinOpenTK_ImGUI
{
	public class infoLine
	{
		public string Value { get; set; }
		public int LineIndex { get; set; }
		public int LineWrapIndex { get; set; }
		public uint PositionStart { get; set; }
		public uint PositionEnd { get; set; }
		public uint ValueLength { get => (uint)Value.Length; }
		public uint LineLength { get => (uint)Math.Min(Value.Length, PositionEnd - PositionStart + 1); }
		public string LineValue { get => Value.Substring(0, (int)LineLength); }
		public static implicit operator string(infoLine ln) => ln.LineValue;
		public override string ToString()
		{
			return $"{LineIndex}{((LineWrapIndex >= 0)?(", " + LineWrapIndex.ToString()):(""))}: [{PositionStart}, {PositionEnd}] \"{LineValue}\"";
		}
	}
	class clsImGuiColorTextEditor
	{
		private Stopwatch CursorBlinkState;
		private bool bolIsActive = false;
		private bool bolIsHovered = false;
		private uint intCursorPosition = 0;
		public Func<Vector4> BackColor { get; set; } = () =>
			ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(ImGuiCol.FrameBg));
		public Func<Vector4> ForeColor { get; set; } = () => 
			ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(ImGuiCol.Text));
		public Func<Vector4> SelectionColor { get; set; } = () => 
			ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(ImGuiCol.TextSelectedBg));
		public Func<Vector4> WhitespaceColor { get; set; } = () => 
			new Vector4(1.0f, 1.0f, 0.0f, 0.5f);
		public bool ShowWhiteSpace { set; get; } = true;
		public bool WordWrap { set; get; } = true;
		public int SelectionLength { set; get; } = 0;
		private string strText = "";
		public string Text { get => strText; set { strText = value; UpdateLines(); } }
		public clsImGuiColorTextEditor()
		{
			Lines = new infoLine[] { new infoLine()
							{
								Value = "",
								LineIndex = 0,
								PositionStart = (uint)0,
								PositionEnd = (uint)0
							} };
			CursorBlinkState = new Stopwatch();
			CursorBlinkState.Start();
		}
		public void UpdateLines()
		{
			Lines = GetLines(strText, WordWrap);
		}
		public static infoLine[] GetLines(string strText, bool bolWordWrap)
		{
			List<infoLine> ret = new List<infoLine>();
			ImFontPtr font = ImGui.GetFont();
			float lineWidth = ImGui.GetWindowContentRegionWidth() - font.FallbackAdvanceX * 2f;
			MatchCollection matches = Regex.Matches(strText, @"(?<Line>.*)?(?<EndLine>(\r\n|\r|\n))?");
			int intLineNumber = 0;
			foreach (Match itm in matches)
			{
				Group grp = itm.Groups["Line"];
				Group grpEnd = itm.Groups["EndLine"];
				if (grp != null)
				{
					if (bolWordWrap)
					{
						float linePos = 0f;
						string strLine = "";
						string strLineText = "";
						int lineIndexStart = 0;
						int lineIndex;
						int intLineWrapNumber = 0;
						for (lineIndex = 0; lineIndex < itm.Value.Length; lineIndex++)
						{
							char c = itm.Value[lineIndex];
							strLine += c;
							if (c != '\r' && c != '\n') strLineText += c;
							linePos += font.GetCharAdvance(c);
							if (linePos >= lineWidth)
							{
								ret.Add(new infoLine()
								{
									Value = strLine + '\r',
									LineIndex = intLineNumber,
									LineWrapIndex = intLineWrapNumber,
									PositionStart = (uint)(grp.Index + lineIndexStart),
									PositionEnd = (uint)(grp.Index + lineIndexStart + Math.Max(0, strLineText.Length - 1))
								});
								lineIndexStart = lineIndex + 1;
								strLine = "";
								strLineText = "";
								linePos = 0f;
								intLineWrapNumber++;
							}
						}
						if (strLine.Length > 0)
						{
							ret.Add(new infoLine()
							{
								Value = strLine,
								LineIndex = intLineNumber,
								LineWrapIndex = (intLineWrapNumber > 0)?intLineWrapNumber:-1,
								PositionStart = (uint)(grp.Index + lineIndexStart),
								PositionEnd = (uint)(grp.Index + lineIndexStart + Math.Max(0, strLineText.Length - 1))
							});
						}
						else
						{
							ret.Add(new infoLine()
							{
								Value = "",
								LineIndex = intLineNumber,
								LineWrapIndex = -1,
								PositionStart = (uint)(grp.Index + lineIndexStart),
								PositionEnd = (uint)(grp.Index + lineIndexStart)
							});
						}
					}
					else
					{
						if (itm.Value.Length > 0)
						{
							ret.Add(new infoLine()
							{
								Value = itm.Value,
								LineIndex = intLineNumber,
								LineWrapIndex = -1,
								PositionStart = (uint)grp.Index,
								PositionEnd = (uint)(grp.Index + Math.Max(0, grp.Length - 1))
							});
						}
						else
						{
							ret.Add(new infoLine()
							{
								Value = "",
								LineIndex = intLineNumber,
								LineWrapIndex = -1,
								PositionStart = (uint)grp.Index,
								PositionEnd = (uint)(grp.Index)
							});
						}
					}
				}
				intLineNumber++;
				if (grpEnd == null) break;
				if (!grpEnd.Success) break;
			}
			return ret.ToArray();
		}
		public static Point GetCursorLocationFromPos(infoLine[] aryLines, Vector2 pos)
		{
			ImFontPtr font = ImGui.GetFont();
			Point ret = new Point();
			ret.Y = (int)(pos.Y / ImGui.GetTextLineHeightWithSpacing());
			infoLine ln = aryLines[Math.Min(Math.Max(ret.Y, 0), aryLines.Length - 1)];
			float posLineChar = 0;
			for (ret.X = 0; ret.X < ln.ValueLength; ret.X++)
			{
				posLineChar += font.GetCharAdvance(ln.Value[ret.X]);
				if (posLineChar > pos.X) break;
			}
			return ret;
		}
		public static uint GetCursorPositionFromPos(infoLine[] aryLines, Vector2 pos)
		{
			return GetCursorPositionFromLocation(aryLines, GetLocationFromPosition(aryLines, pos));
		}
		public static (Point Loc, uint Idx) GetLocationFromCursorPos(infoLine[] aryLines, uint pos)
		{
			Point ret = new Point((int)aryLines[aryLines.Length - 1].ValueLength, aryLines.Length - 1);
			uint retIdx = pos;
			for (int itr = 0; itr < aryLines.Length; itr++)
			{
				if (pos >= aryLines[itr].PositionStart)
				{
					if (pos <= aryLines[itr].PositionEnd)
					{
						return (new Point((int)(pos - aryLines[itr].PositionStart), itr), pos);
					}
					else
					{
						if (itr < aryLines.Length - 1)
						{
							retIdx = aryLines[itr + 1].PositionStart;
							ret = new Point(0, itr + 1);
						}
						else
						{
							retIdx = aryLines[itr].PositionEnd + 1;
							ret = new Point((int)aryLines[aryLines.Length - 1].LineLength, itr);
						}
						
					}
				}
			}
			return (ret, retIdx);
		}
		public static uint GetCursorPositionFromLocation(infoLine[] aryLines, Point pos)
		{
			if (pos.X < 0) { pos.Y--; }
			if (pos.Y < 0) pos.Y = 0;
			if (pos.Y > aryLines.Length - 1) { pos.Y = aryLines.Length - 1; }
			if (pos.X < 0) pos.X += (int)aryLines[pos.Y].LineLength;
			if (pos.X > aryLines[pos.Y].LineLength) { pos.Y++; }
			if (pos.Y > aryLines.Length - 1) { pos.Y = aryLines.Length - 1; }
			if (pos.X > aryLines[pos.Y].LineLength) pos.X -= (int)aryLines[pos.Y].LineLength;
			return (uint)(aryLines[pos.Y].PositionStart + pos.X);
		}
		public static Vector2 GetPositionFromCursorPos(infoLine[] aryLines, Point pos)
		{
			ImFontPtr font = ImGui.GetFont();
			return new Vector2(aryLines[pos.Y].Value.Substring(0, pos.X)
				.Aggregate(0f, (llen, ch) => llen + font.GetCharAdvance(ch)),
				pos.Y * ImGui.GetTextLineHeightWithSpacing());
		}
		public static Point GetLocationFromPosition(infoLine[] aryLines, Vector2 pos)
		{
			ImFontPtr font = ImGui.GetFont();
			Point locationNew = new Point();
			locationNew.Y = (int)(pos.Y / ImGui.GetTextLineHeightWithSpacing());
			locationNew.Y = Math.Min(Math.Max(locationNew.Y, 0), aryLines.Length - 1);
			infoLine ln = aryLines[locationNew.Y];
			float posLineChar = 0;
			for (locationNew.X = 0; locationNew.X < ln.ValueLength; locationNew.X++)
			{
				posLineChar += font.GetCharAdvance(ln.Value[locationNew.X]);
				if (posLineChar > pos.X) break;
			}
			return locationNew;
		}
		public uint CursorPosition
		{
			get => intCursorPosition;
			set { intCursorPosition = GetLocationFromCursorPos(Lines, value).Idx; }
		}
		public Point CursorLocation
		{
			get => GetLocationFromCursorPos(Lines, intCursorPosition).Loc;
			set { CursorPosition = GetCursorPositionFromLocation(Lines, value); }
		}
		public Vector2 CursorContentLocation
		{
			get => GetPositionFromCursorPos(Lines, CursorLocation);
			set { CursorLocation = GetLocationFromPosition(Lines, value); }
		}
		public int CursorLine
		{
			get => CursorLocation.Y;
			set { CursorLocation = new Point(CursorLocation.X, value); }
		}
		public int CursorLinePosition
		{
			get => CursorLocation.X;
			set { CursorLocation = new Point(value, CursorLocation.Y); }
		}
		public int SelectionStart { get => Math.Max(0, Math.Min((int)intCursorPosition, (int)intCursorPosition + SelectionLength)); }
		public int SelectionEnd { get => Math.Max(0, Math.Max((int)intCursorPosition, (int)intCursorPosition + SelectionLength)); }
		public string SelectedText { get => Text.Substring(SelectionStart, SelectionEnd - SelectionStart); }
		public infoLine[] Lines { private set; get; }
		public infoLine[] SelectedLines
		{
			get
			{
				List<infoLine> ret = new List<infoLine>();
				infoLine[] aryLines = Lines;
				uint posStart = (uint)SelectionStart;
				uint posEnd = (uint)SelectionEnd;
				Point locStart = GetLocationFromCursorPos(aryLines, posStart).Loc;
				Point locEnd = GetLocationFromCursorPos(aryLines, posEnd).Loc;
				for (int itrLine = locStart.Y; itrLine <= locEnd.Y; itrLine++)
				{
					if ((posStart >= aryLines[itrLine].PositionStart && posStart <= aryLines[itrLine].PositionEnd) ||
					   (posEnd >= aryLines[itrLine].PositionStart && posEnd <= aryLines[itrLine].PositionEnd) ||
					   (posStart <= aryLines[itrLine].PositionStart && posEnd >= aryLines[itrLine].PositionEnd))
					{
						int st = (int)Math.Max(posStart, aryLines[itrLine].PositionStart);
						int ed = (int)Math.Min(posEnd, aryLines[itrLine].PositionEnd);
						int len = ed - st;
						if (len > 0)
						{
							ret.Add(new infoLine()
							{
								Value = Text.Substring(st, len + ((posEnd > ed) ? 1 : 0)),
								PositionStart = (uint)st,
								PositionEnd = (uint)ed
							});
						}
					}
				}
				return ret.ToArray();
			}
		}
		public Size CharDimensions
		{
			get => new Size(Lines.Aggregate(0, (cnt, itm) => Math.Max((int)itm.LineLength, cnt)), Lines.Length);
		}
		public Vector2 ContentDimensions
		{
			get 
			{
				ImFontPtr font = ImGui.GetFont();
				return new Vector2(
					Lines.Aggregate(0f, (cnt, itm) => Math.Max(
						itm.LineValue.Aggregate(0f, (llen, ch) => llen + font.GetCharAdvance(ch)), cnt)), 
						Lines.Length * ImGui.GetTextLineHeightWithSpacing()
				);
			}
		}
		private Vector2 szPrevSize = new Vector2();
		private int intMouseInitPress = 0;
		public void UpdateInputState(clsInputState ioState)
		{
			bolIsActive = ImGui.IsWindowFocused();
			bolIsHovered = ImGui.IsWindowHovered();
			if (bolIsActive)
			{
				if(!CursorBlinkState.IsRunning)
				{
					CursorBlinkState.Start();
				}
				if (CursorBlinkState.Elapsed.TotalSeconds > 1) CursorBlinkState.Reset();
			}
			else
			{
				if(CursorBlinkState.IsRunning)
				{
					CursorBlinkState.Stop();
					CursorBlinkState.Reset();
				}
			}
			Vector2 posWindow = ImGui.GetWindowPos();
			Vector2 szWindow = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();
			szWindow.X += 6;
			Vector2 endWindow = posWindow + szWindow;
			Vector2 posMove = new Vector2(ImGui.GetFont().FallbackAdvanceX, ImGui.GetTextLineHeightWithSpacing());
			Vector2 posContent = new Vector2(ImGui.GetScrollX(), ImGui.GetScrollY());
			Vector2 posMouse = ioState.MouseState.Pos;
			Vector2 posMouseContent = (posMouse - posWindow) + posContent;
			if (szWindow.X != szPrevSize.X || szWindow.Y != szPrevSize.Y)
			{
				UpdateLines();
			}
			szPrevSize = szWindow;
			if (bolIsHovered)
			{
				Point locMouseContent = GetCursorLocationFromPos(Lines, posMouseContent);
				locMouseContent.Y = Math.Min(Lines.Length - 1, locMouseContent.Y);
				locMouseContent.X = Math.Min((int)Lines[locMouseContent.Y].LineLength,  locMouseContent.X);
				uint intMouseContent; intMouseContent = GetCursorPositionFromLocation(Lines, locMouseContent);
				if (posMouse.X >= posWindow.X && posMouse.X <= endWindow.X &&
				   posMouse.Y >= posWindow.Y && posMouse.Y <= endWindow.Y)
				{
					ImGui.SetMouseCursor(ImGuiMouseCursor.TextInput);
					if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
					{
						if (ioState.KeyboardState[(int)Keys.ShiftKey])
						{
							SelectionLength = (int)intMouseContent - (int)intCursorPosition;
						} 
						else
						{
							SelectionLength = 0;
							CursorLocation = locMouseContent;
							intMouseInitPress = (int)intCursorPosition;
							CursorBlinkState.Reset();
						}
					} 
					else if(ImGui.IsMouseDragging(ImGuiMouseButton.Left))
					{
						SelectionLength = intMouseInitPress - (int)intMouseContent;
						CursorPosition = intMouseContent;
						CursorBlinkState.Reset();
					}
				}
			}
			if (bolIsActive)
			{
				bool bolCursorUpdate = false;
				if (ioState.KeyboardState[(int)Keys.Right]) { ioState.KeyboardState[(int)Keys.Right] = false; CursorLinePosition++; bolCursorUpdate = true; }
				if (ioState.KeyboardState[(int)Keys.Left]) { ioState.KeyboardState[(int)Keys.Left] = false; CursorLinePosition--; bolCursorUpdate = true; }
				if (ioState.KeyboardState[(int)Keys.Down]) { ioState.KeyboardState[(int)Keys.Down] = false; CursorLine++; bolCursorUpdate = true; }
				if (ioState.KeyboardState[(int)Keys.Up]) { ioState.KeyboardState[(int)Keys.Up] = false; CursorLine--; bolCursorUpdate = true; }
				if (ioState.KeyboardState[(int)Keys.ControlKey] && ioState.KeyboardState[(int)Keys.C])
				{
					ImGui.SetClipboardText(SelectedText);
					ioState.KeyboardState[(int)Keys.C] = false;
					ioState.InputChars = "";
				} else if (ioState.KeyboardState[(int)Keys.ControlKey] && ioState.KeyboardState[(int)Keys.X])
				{
					if (SelectionLength != 0)
					{
						ImGui.SetClipboardText(SelectedText);
						Text = Text.Remove(SelectionStart, Math.Abs(SelectionLength));
						SelectionLength = 0;
						ioState.KeyboardState[(int)Keys.X] = false;
						ioState.InputChars = "";
						bolCursorUpdate = true;
					}
				} else if (ioState.KeyboardState[(int)Keys.ControlKey] && ioState.KeyboardState[(int)Keys.V])
				{
					string str = "";
					try
					{
						str = ImGui.GetClipboardText();
					} catch(NullReferenceException errNull)
					{
						str = "";
						Console.WriteLine(errNull);
					}
					if (str != "")
					{
						if (SelectionLength != 0)
						{
							Text = Text.Remove(SelectionStart, Math.Abs(SelectionLength));
							SelectionLength = 0;
						}
						Text = Text.Insert((int)intCursorPosition, str);
						CursorPosition += (uint)str.Length;
						ioState.KeyboardState[(int)Keys.V] = false;
						ioState.InputChars = "";
						bolCursorUpdate = true;
					}
				}
				if (ioState.KeyboardState[(int)Keys.Delete]) 
				{
					if (SelectionLength != 0)
					{
						Text = Text.Remove(SelectionStart, Math.Abs(SelectionLength));
						SelectionLength = 0;
					} else
					{
						if (intCursorPosition <= Text.Length - 2 && Text.Substring((int)intCursorPosition, 2) == "\r\n")
							Text = Text.Remove((int)intCursorPosition, 2);
						else if (CursorPosition < Text.Length)
							Text = Text.Remove((int)intCursorPosition, 1);
					}
					ioState.KeyboardState[(int)Keys.Delete] = false;
					bolCursorUpdate = true; 
				}
				if (ioState.KeyboardState[(int)Keys.Back]) 
				{
					if (SelectionLength != 0)
					{
						Text = Text.Remove(SelectionStart, Math.Abs(SelectionLength));
						SelectionLength = 0;
					} else
					{
						if ((int)intCursorPosition - 2 >= 0 && Text.Substring((int)intCursorPosition - 2, 2) == "\r\n")
						{ Text = Text.Remove((int)intCursorPosition - 2, 2); CursorPosition -= 2; }
						else if ((int)intCursorPosition > 0)
						{ Text = Text.Remove((int)intCursorPosition - 1, 1); CursorPosition--; }
					}
					ioState.KeyboardState[(int)Keys.Back] = false;
					bolCursorUpdate = true; 
				}
				if (ioState.KeyboardState[(int)Keys.Tab])
				{
					Text = Text.Insert((int)intCursorPosition, "\t"); CursorLinePosition++;
					ioState.KeyboardState[(int)Keys.Tab] = false;
					bolCursorUpdate = true;
				}
				if (ioState.KeyboardState[(int)Keys.Enter]) 
				{
					if (SelectionLength != 0)
					{
						Text = Text.Remove(SelectionStart, Math.Abs(SelectionLength));
						SelectionLength = 0;
					}
					Text = Text.Insert((int)intCursorPosition, "\r\n");
					CursorPosition += 2;
					ioState.KeyboardState[(int)Keys.Enter] = false;
					bolCursorUpdate = true;
				}
				char[] strInputChars = Array.FindAll(ioState.InputChars.ToCharArray(), cItm => cItm >= 32);
				if(strInputChars.Length > 0)
				{
					if (SelectionLength != 0)
					{
						Text = Text.Remove(SelectionStart, Math.Abs(SelectionLength));
						SelectionLength = 0;
					}
					foreach (char c in strInputChars)
					{
						if (c >= 32)
						{
							Text = Text.Insert((int)intCursorPosition, c.ToString());
							CursorPosition++;
							bolCursorUpdate = true;
						}
					}
				}
				if (bolCursorUpdate)
				{
					Vector2 posCursor = posWindow + CursorContentLocation - posContent;
					if(posCursor.X <= posWindow.X)
					{
						ImGui.SetScrollX(posContent.X - (posWindow.X - posCursor.X));
					}
					if(posCursor.X + posMove.X*2 >= endWindow.X)
					{
						ImGui.SetScrollX(posContent.X + ((posCursor.X + posMove.X*2) - endWindow.X));
					}
					if (posCursor.Y <= posWindow.Y)
					{
						ImGui.SetScrollY(posContent.Y - (posWindow.Y - posCursor.Y));
					}
					if (posCursor.Y + posMove.Y*2 >= endWindow.Y)
					{
						ImGui.SetScrollY(posContent.Y + ((posCursor.Y + posMove.Y*2) - endWindow.Y));
					}
					CursorBlinkState.Reset();
				}
			}
		}
		public void Render()
		{
			ImFontPtr fontCurrent = ImGui.GetFont();
			ImDrawListPtr draw = ImGui.GetWindowDrawList();
			Vector2 posMove = new Vector2(16, 16);
			posMove.X = fontCurrent.FallbackAdvanceX;
			posMove.Y = ImGui.GetTextLineHeightWithSpacing();
			Vector2 posWindow = ImGui.GetWindowPos();
			Vector2 szWindow = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();
			szWindow.Y += posMove.Y;
			Vector2 endWindow = posWindow + szWindow;
			posWindow.X += 8;
			posWindow.Y += 6;
			Size dimChars = CharDimensions;
			infoLine[] aryLines = Lines;
			Vector2 szContent = ContentDimensions;
			Vector2 posContent = new Vector2(ImGui.GetScrollX(), ImGui.GetScrollY());
			ImGui.Dummy(szContent);
			Vector2 posScr = posWindow - posContent;
			Point posCursor = CursorLocation;
			uint intRow = 0;
			uint intColumn;
			uint intColorFore = ImGui.ColorConvertFloat4ToU32(ForeColor());
			uint intColorBack = ImGui.ColorConvertFloat4ToU32(BackColor());
			uint intColorWhitespace = ImGui.ColorConvertFloat4ToU32(WhitespaceColor());
			draw.AddRectFilled(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), intColorBack);
			infoLine[] arySelectedLines = SelectedLines;
			uint colorSel = ImGui.ColorConvertFloat4ToU32(SelectionColor());
			for (int itrLine = 0; itrLine < arySelectedLines.Length; itrLine++)
			{
				string strSelected = arySelectedLines[itrLine].LineValue;
				uint intStart = arySelectedLines[itrLine].PositionStart;
				uint intEnd = arySelectedLines[itrLine].PositionEnd;
				Point locLine = GetLocationFromCursorPos(aryLines, intStart).Loc;
				Vector2 posLineRectMin = GetPositionFromCursorPos(aryLines, locLine) - posContent;
				locLine = GetLocationFromCursorPos(aryLines, intEnd).Loc;
				Vector2 posLineRectMax = GetPositionFromCursorPos(aryLines, locLine) - posContent;
				float fEnd = fontCurrent.GetCharAdvance(strSelected[strSelected.Length - 1]);
				fEnd = (intStart + strSelected.Length > intEnd) ? fEnd : 0f;
				posLineRectMin += new Vector2(0, -3);
				posLineRectMax += new Vector2(fEnd, posMove.Y - 3);
				posLineRectMin = new Vector2(Math.Min(Math.Max(0, posLineRectMin.X), szWindow.X), Math.Min(Math.Max(-3, posLineRectMin.Y), szWindow.Y));
				posLineRectMax = new Vector2(Math.Min(Math.Max(0, posLineRectMax.X), szWindow.X), Math.Min(Math.Max(-3, posLineRectMax.Y), szWindow.Y));
				Vector2 szLineRect = posLineRectMax - posLineRectMin;
				if (szLineRect.X > 0 && szLineRect.Y > 0)
				{
					draw.AddRectFilled(posWindow + posLineRectMin, posWindow + posLineRectMax, colorSel);
				}
			}
			while (posScr.Y <= posWindow.Y + szContent.Y)
			{
				intColumn = 0;
				posScr.X = posWindow.X - posContent.X;
				if (intRow < dimChars.Height && posScr.Y >= posWindow.Y - posMove.Y && posScr.Y <= endWindow.Y)
				{
					string strLine = aryLines[intRow].Value;
					while (posScr.X <= posWindow.X + szWindow.X)
					{
						if (intColumn < strLine.Length && posScr.X >= posWindow.X - posMove.X && posScr.X <= endWindow.X)
						{
							char charItm = strLine[(int)intColumn];
							posMove.X = fontCurrent.GetCharAdvance(charItm);
							switch (charItm)
							{
								case '\r':
									if (ShowWhiteSpace) draw.AddText(fontCurrent, fontCurrent.FontSize * 0.5f, posScr, intColorWhitespace, "CR");
									break;
								case '\n':
									if (ShowWhiteSpace) draw.AddText(fontCurrent, fontCurrent.FontSize * 0.5f, posScr, intColorWhitespace, "LF");
									break;
								case '\t':
									if (ShowWhiteSpace) draw.AddText(fontCurrent, fontCurrent.FontSize * 0.75f, posScr, intColorWhitespace, "[Tab]");
									ImGui.Indent(1);
									break;
								case ' ':
									if (ShowWhiteSpace) draw.AddRect(posScr + posMove * new Vector2(0.25f, 0.25f), posScr + posMove * new Vector2(0.75f, 0.5f), intColorWhitespace, 0, ImDrawCornerFlags.None, 1);
									break;
								default:
									draw.AddText(posScr, intColorFore, charItm.ToString());
									break;
							}
						}
						if (posCursor.X == intColumn && posCursor.Y == intRow)
						{
							if (CursorBlinkState.Elapsed.TotalSeconds <= 0.5)
							{
								draw.AddTriangle(posScr + new Vector2(-3, -2), posScr + new Vector2(5, -2), posScr + new Vector2(1, 4), intColorFore, 2.5f);
								draw.AddRectFilled(posScr + new Vector2(0, 4), posScr + new Vector2(2, posMove.Y - 4), intColorFore);
							}
						}
						posScr.X += posMove.X;
						intColumn++;
					}
				}
				posScr.Y += posMove.Y;
				intRow++;
			}
		}
	}
}
