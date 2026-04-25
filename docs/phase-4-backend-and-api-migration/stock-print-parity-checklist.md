# Stock Print Parity Checklist

Reference artifact: legacy stock PDF sample (report viewer screenshot provided during change proposal).

## Header and summary parity

- [ ] Stock Number appears and matches selected product
- [ ] Product Code appears and matches selected product
- [ ] Product Name appears and matches selected product
- [ ] Production Info appears and wraps correctly
- [ ] Remarks appears and wraps correctly
- [ ] MOQ value appears
- [ ] Balance value appears

## Movement table parity

- [ ] Columns appear in expected order: row/date/reference/qty/balance/modified on/modified by
- [ ] Row order is newest-first by movement date then modified timestamp
- [ ] Row numbering starts at 1 and increments by 1
- [ ] Quantity signs (+/-) are preserved
- [ ] Running balance values are correct by row

## Multilingual rendering parity

- [ ] Simplified Chinese product values render without replacement glyphs
- [ ] Traditional Chinese product values render without replacement glyphs
- [ ] English + CJK mixed strings stay aligned and legible

## Sign-off

- [ ] Product owner sign-off
- [ ] QA sign-off
- [ ] Release readiness acknowledged
