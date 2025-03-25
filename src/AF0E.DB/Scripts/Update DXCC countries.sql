/*
select distinct l.COL_COUNTRY
               ,l.COL_CALL
  from TABLE_HRD_CONTACTS_V01 l where l.COL_COUNTRY is not null and l.COL_COUNTRY <> '[none]' and l.COL_COUNTRY not in (select EntityName from Dxcc) and not exists (select 1 from Dxcc where AltNames like '%|' + replace(l.COL_COUNTRY, '[', '[ [ ]') + '|%') order by l.COL_COUNTRY
*/
/*
select l.COL_COUNTRY, l.*  from TABLE_HRD_CONTACTS_V01 l where l.COL_COUNTRY is not null and l.COL_COUNTRY <> '[none]' and l.COL_COUNTRY not in (select EntityName from Dxcc)
*/
/*
select * from Dxcc order by EntityName
*/

--begin tran
update TABLE_HRD_CONTACTS_V01
   set COL_COUNTRY = d.EntityName
  from Dxcc d
 where COL_COUNTRY is not null
   and COL_COUNTRY <> '[none]'
   and not exists (select 1 from Dxcc where EntityName = COL_COUNTRY)
   and d.AltNames like '%|' + replace(COL_COUNTRY, '[', '[ [ ]') + '|%'
--commit
--rollback
