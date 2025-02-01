--select COL_COUNTRY, * from TABLE_HRD_CONTACTS_V01 where COL_COUNTRY is not null and COL_COUNTRY <> '[none]' and COL_COUNTRY not in (select EntityName from Dxcc)
--begin tran
update TABLE_HRD_CONTACTS_V01
   set COL_COUNTRY = d.EntityName
  from Dxcc d
 where COL_COUNTRY is not null
   and COL_COUNTRY <> '[none]'
   and not exists (select 1 from Dxcc where EntityName = COL_COUNTRY)
   and d.AltNames like '%|' + COL_COUNTRY + '|%'
--commit
--rollback
