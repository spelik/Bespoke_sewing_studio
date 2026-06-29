export interface CsvColumn<TItem> {
  header: string;
  value(item: TItem): string | number | null | undefined;
}

export function downloadCsv<TItem>(
  fileName: string,
  rows: readonly TItem[],
  columns: readonly CsvColumn<TItem>[],
): void {
  const csvRows = [
    columns.map((column) => escapeCsvCell(column.header)).join(","),
    ...rows.map((row) =>
      columns
        .map((column) => escapeCsvCell(column.value(row)))
        .join(","),
    ),
  ];

  const blob = new Blob([`\uFEFF${csvRows.join("\r\n")}`], {
    type: "text/csv;charset=utf-8",
  });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

export function createCsvFileName(prefix: string, date = new Date()): string {
  const stamp = date
    .toISOString()
    .slice(0, 19)
    .replace(/[-:]/g, "")
    .replace("T", "-");

  return `${prefix}-${stamp}.csv`;
}

function escapeCsvCell(value: string | number | null | undefined): string {
  const text = value === null || value === undefined ? "" : String(value);
  const escapedText = text.replace(/"/g, '""');

  return /[",\r\n]/.test(escapedText) ? `"${escapedText}"` : escapedText;
}
