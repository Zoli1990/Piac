<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import client from '../api/client'
import TorzsadatModal from '../components/TorzsadatModal.vue'
import ZoldsegModal from '../components/ZoldsegModal.vue'
import Gyorskereso from '../components/Gyorskereso.vue'

const items=ref([]),partnerek=ref([]),zoldsegek=ref([]),rekesztipusok=ref([]),raktar=ref([])
const hiba=ref(''),betolt=ref(true),datum=ref(today())
const showPartnerModal=ref(false),showZoldsegModal=ref(false),showRekeszModal=ref(false),editId=ref(null),editForm=ref({})
const form=ref({sajatTermek:false,partnerId:'',zoldsegId:'',rekeszTipusId:'',mennyiseg:'',fizetve:false,adottRekeszDb:0,egysegar:'',megjegyzes:'',helyszin:'Kocsi'})
const athelyezesStock=ref(null),athelyezesForm=ref({mennyiseg:1,celDatum:''}),athelyezesHiba=ref('')
const raktarDetail=ref(null)
const fotoInput=ref(null),fotoFeltoltesFolyamatban=ref(false),fotoMentveJelzes=ref(false)
function today(){return new Date().toISOString().slice(0,10)}
function fmtDate(d){return new Date(d+'T00:00:00').toLocaleDateString('hu-HU')}
const apiRoot=client.defaults.baseURL.replace(/\/api\/?$/,'')
function kepSrc(u){return u?.startsWith('http')?u:apiRoot+u}
function clampDigits(v,n){const d=String(v??'').replace(/[^0-9]/g,'').slice(0,n);return d===''?'':Number(d)}
async function torzsadatok(){try{const[p,z,r]=await Promise.all([client.get('/partnerek'),client.get('/zoldsegek'),client.get('/rekesztipusok')]);partnerek.value=p.data;zoldsegek.value=z.data;rekesztipusok.value=r.data}catch{hiba.value='Nem sikerült betölteni a törzsadatokat.'}}
async function frissitRaktar(){try{raktar.value=(await client.get('/felvasarlas/raktar-csoportos')).data}catch{hiba.value='Nem sikerült betölteni a raktárkészletet.'}}
async function frissit(){betolt.value=true;hiba.value='';try{items.value=(await client.get('/felvasarlas',{params:{datum:datum.value}})).data}catch{hiba.value='Nem sikerült betölteni a felvásárlásokat.'}finally{betolt.value=false}await frissitRaktar()}
onMounted(async()=>{await torzsadatok();await frissit()});watch(datum,frissit)
watch(()=>form.value.zoldsegId,id=>{const z=zoldsegek.value.find(x=>x.id===Number(id));if(z?.alapertelmezettRekeszTipusId)form.value.rekeszTipusId=z.alapertelmezettRekeszTipusId})
function adottRekeszValt(e){form.value.adottRekeszDb=clampDigits(e.target.value,3) ?? 0}
function zoldsegLetrehozva(z){zoldsegek.value.push(z)}
function rekeszLetrehozva(r){rekesztipusok.value.push(r)}
function partnerLetrehozva(p){partnerek.value.push(p)}

function triggerFotoInput(){hiba.value='';if(!form.value.zoldsegId){hiba.value='Előbb válassz vagy ments egy zöldséget.';return}fotoInput.value.click()}
async function fotoValasztva(e){
  const file=e.target.files[0];e.target.value=''
  if(!file)return
  if(!file.type.startsWith('image/'))return hiba.value='Csak kép tölthető fel.'
  if(file.size>5_000_000)return hiba.value='A kép mérete legfeljebb 5 MB lehet.'
  hiba.value='';fotoFeltoltesFolyamatban.value=true
  try{
    const fd=new FormData();fd.append('kep',file)
    const res=await client.post(`/zoldsegek/${form.value.zoldsegId}/kep`,fd,{headers:{'Content-Type':'multipart/form-data'}})
    const idx=zoldsegek.value.findIndex(z=>z.id===form.value.zoldsegId)
    if(idx>=0)zoldsegek.value[idx]={...zoldsegek.value[idx],kepUrl:res.data.kepUrl}
    fotoMentveJelzes.value=true;setTimeout(()=>fotoMentveJelzes.value=false,2500)
  }catch(e2){hiba.value=e2.response?.data?.message||'Kép feltöltése sikertelen.'}
  finally{fotoFeltoltesFolyamatban.value=false}
}

function csempeStilus(zoldsegId){
  const z=zoldsegek.value.find(x=>x.id===Number(zoldsegId))
  if(!z?.kepUrl)return {}
  const url=z.kepUrl.startsWith('http')?z.kepUrl:apiRoot+z.kepUrl
  return {backgroundImage:`url('${url}')`,backgroundSize:'cover',backgroundPosition:'center'}
}

// A napi felvásárlási rekordok az adatbázisban külön maradnak.
// A listanézetben csak akkor vonjuk össze őket, ha a zöldség, a rekesztípus
// és a felvásárlási ár azonos. Partner, napi sorszám és időpont nem része a kulcsnak.
const csoportositottFelvasarlas=computed(()=>{
  const groups=new Map()
  for(const item of items.value){
    const ar=item.egysegar==null?null:Number(item.egysegar)
    const key=`${item.zoldsegId??item.zoldsegNev}|${item.rekeszTipusId??item.rekeszTipus}|${ar==null?'null':ar}`
    if(!groups.has(key)){
      groups.set(key,{
        key,
        zoldsegId:item.zoldsegId,
        zoldsegNev:item.zoldsegNev,
        zoldsegKepUrl:item.zoldsegKepUrl,
        rekeszTipusId:item.rekeszTipusId,
        rekeszTipus:item.rekeszTipus,
        egysegar:ar,
        mennyiseg:0,
        items:[]
      })
    }
    const group=groups.get(key)
    group.mennyiseg+=Number(item.mennyiseg)||0
    group.items.push(item)
  }
  return Array.from(groups.values())
})

const felvDetail=ref(null)
const felvCsoportDetail=ref(null)
function nyitFelvDetail(item){felvDetail.value=item;szerkesztesInditasa(item)}
function nyitFelvCsoportDetail(group){felvCsoportDetail.value=group}
function zarFelvCsoportDetail(){felvCsoportDetail.value=null}
function szerkesztCsoportTetelet(item){zarFelvCsoportDetail();nyitFelvDetail(item)}
function zarFelvDetail(){felvDetail.value=null;editId.value=null}
async function mentesModalbol(){await mentSzerkesztes(felvDetail.value.id);if(!hiba.value)zarFelvDetail()}
async function torolModalbol(){const it=felvDetail.value;if(!confirm(`Törlöd a #${it.napiSorszam} tételt (${it.zoldsegNev})?`))return;try{await client.delete(`/felvasarlas/${it.id}`);zarFelvDetail();await frissit()}catch{hiba.value='Törlés sikertelen.'}}

function nyitRaktarDetail(s){raktarDetail.value=s}
function zarRaktarDetail(){raktarDetail.value=null}
function nyitForrasAthelyezes(s,forras){
  athelyezesStock.value={...forras,zoldsegNev:s.zoldsegNev,rekeszTipus:s.rekeszTipus}
  athelyezesForm.value={mennyiseg:forras.maradt,celDatum:today()}
  athelyezesHiba.value=''
}
function nyitAthelyezes(s){
  if(s.forrasTetelek?.length===1){nyitForrasAthelyezes(s,s.forrasTetelek[0]);return}
  nyitRaktarDetail(s)
}
function zarAthelyezes(){athelyezesStock.value=null}
async function athelyezes(){
  athelyezesHiba.value=''
  const s=athelyezesStock.value
  if(!s)return
  if(!athelyezesForm.value.mennyiseg||Number(athelyezesForm.value.mennyiseg)<=0)return athelyezesHiba.value='A mennyiség legalább 1 kell legyen.'
  if(Number(athelyezesForm.value.mennyiseg)>s.maradt)return athelyezesHiba.value='Nincs ennyi a kiválasztott forrástételben.'
  try{
    await client.post('/felvasarlas/raktarbol-kocsira',{felvasarlasTetelId:s.id,mennyiseg:Number(athelyezesForm.value.mennyiseg),celDatum:athelyezesForm.value.celDatum})
    zarAthelyezes();zarRaktarDetail();await frissit()
  }catch(e){athelyezesHiba.value=e.response?.data?.message||'Áthelyezés sikertelen.'}
}
async function ujTetel(){hiba.value='';if(!form.value.sajatTermek&&!form.value.partnerId)return hiba.value='Vásárolt árunál az eladó kiválasztása kötelező.';if(!form.value.sajatTermek&&(form.value.egysegar===''||form.value.egysegar==null))return hiba.value='Vásárolt árunál az egységár megadása kötelező.';if(!form.value.zoldsegId||!form.value.rekeszTipusId)return hiba.value='Zöldség és rekesztípus kiválasztása kötelező.';if(!form.value.mennyiseg||Number(form.value.mennyiseg)<=0)return hiba.value='A mennyiség legalább 1 kell legyen.';if(Number(form.value.adottRekeszDb)>Number(form.value.mennyiseg))return hiba.value='Az adott rekesz nem lehet több a mennyiségnél.';try{await client.post('/felvasarlas',{sajatTermek:form.value.sajatTermek,partnerId:form.value.sajatTermek?null:Number(form.value.partnerId),zoldsegId:Number(form.value.zoldsegId),rekeszTipusId:Number(form.value.rekeszTipusId),mennyiseg:Number(form.value.mennyiseg),fizetve:form.value.fizetve,adottRekeszDb:Number(form.value.adottRekeszDb)||0,egysegar:form.value.egysegar===''?null:Number(form.value.egysegar),megjegyzes:form.value.megjegyzes.trim()||null,datum:datum.value,helyszin:form.value.helyszin});form.value={...form.value,zoldsegId:'',mennyiseg:'',fizetve:false,adottRekeszDb:0,egysegar:'',megjegyzes:''};await frissit()}catch(e){hiba.value=e.response?.data?.message||'Mentés sikertelen.'}}
function szerkesztesInditasa(i){editId.value=i.id;editForm.value={sajatTermek:i.sajatTermek,partnerId:i.partnerId||'',zoldsegId:i.zoldsegId,rekeszTipusId:i.rekeszTipusId,mennyiseg:i.mennyiseg,fizetve:i.fizetve,adottRekeszDb:i.adottRekeszDb,egysegar:i.egysegar??'',megjegyzes:i.megjegyzes||'',helyszin:i.helyszin||'Kocsi',datum:i.datum}}
async function mentSzerkesztes(id){hiba.value='';try{await client.put(`/felvasarlas/${id}`,{sajatTermek:editForm.value.sajatTermek,partnerId:editForm.value.sajatTermek?null:Number(editForm.value.partnerId),zoldsegId:Number(editForm.value.zoldsegId),rekeszTipusId:Number(editForm.value.rekeszTipusId),mennyiseg:Number(editForm.value.mennyiseg),fizetve:editForm.value.fizetve,adottRekeszDb:Number(editForm.value.adottRekeszDb)||0,egysegar:editForm.value.egysegar===''?null:Number(editForm.value.egysegar),megjegyzes:editForm.value.megjegyzes.trim()||null,datum:editForm.value.datum,helyszin:editForm.value.helyszin});editId.value=null;await frissit()}catch(e){hiba.value=e.response?.data?.message||'Mentés sikertelen.'}}
async function torol(i){if(!confirm(`Törlöd a #${i.napiSorszam} tételt (${i.zoldsegNev})?`))return;try{await client.delete(`/felvasarlas/${i.id}`);await frissit()}catch{hiba.value='Törlés sikertelen.'}}
</script>

<template>
  <div class="toolbar-row">
    <label class="date-picker">📅 Dátum <input v-model="datum" type="date" /></label>
  </div>
  <div class="grid">
    <div class="card">
      <div class="section-head"><h2>Új felvásárlás</h2></div>
      <label class="checkbox"><input v-model="form.sajatTermek" type="checkbox" /> Saját termés (nincs eladó)</label>
      <label>Hová kerül<div class="helyszin-radio">
        <label class="radio-pill"><input v-model="form.helyszin" type="radio" value="Kocsi" /> 🚚 Kocsi</label>
        <label class="radio-pill"><input v-model="form.helyszin" type="radio" value="Raktar" /> 🏬 Raktár</label>
      </div></label>
      <label v-if="!form.sajatTermek">Eladó<Gyorskereso v-model="form.partnerId" :items="partnerek" post-path="/partnerek" mentes-label="Eladó mentése" placeholder="Kezdj el gépelni…" @created="partnerLetrehozva" /></label>
      <label>Zöldség<Gyorskereso v-model="form.zoldsegId" :items="zoldsegek" post-path="/zoldsegek" mentes-label="Zöldség mentése" placeholder="Kezdj el gépelni…" @created="zoldsegLetrehozva" /></label>
      <div class="foto-row">
        <button type="button" class="foto-btn" :disabled="fotoFeltoltesFolyamatban" @click="triggerFotoInput">
          <span v-if="fotoFeltoltesFolyamatban">⏳ Feltöltés…</span>
          <span v-else-if="fotoMentveJelzes">✅ Kép mentve</span>
          <span v-else>📸 Kép feltöltése ehhez a zöldséghez</span>
        </button>
        <input ref="fotoInput" type="file" accept="image/*" capture="environment" style="display:none" @change="fotoValasztva" />
      </div>
      <label>Rekesztípus<Gyorskereso v-model="form.rekeszTipusId" :items="rekesztipusok" post-path="/rekesztipusok" mentes-label="Rekesztípus mentése" placeholder="pl. M10" @created="rekeszLetrehozva" /></label>
      <div class="row">
        <label>Mennyiség (db)<input :value="form.mennyiseg" @input="form.mennyiseg=clampDigits($event.target.value,3)" inputmode="numeric" pattern="[0-9]*" class="narrow-3" /></label>
        <label>{{form.sajatTermek?'Becsült önköltség (Ft/db)':'Egységár (Ft/db)'}}<input :value="form.egysegar" @input="form.egysegar=clampDigits($event.target.value,5)" inputmode="numeric" pattern="[0-9]*" class="narrow-5" :required="!form.sajatTermek" :placeholder="form.sajatTermek?'opcionális':'kötelező'" /></label>
      </div>
      <label v-if="!form.sajatTermek">Adott üres rekesz (db)<input :value="form.adottRekeszDb" @input="adottRekeszValt" inputmode="numeric" pattern="[0-9]*" class="narrow-3" /></label>
      <button type="button" class="toggle-pill" :class="{active:form.fizetve}" @click="form.fizetve=!form.fizetve">{{form.fizetve?'✅ Fizetve':'⬜ Fizetve'}}</button>
      <label>Megjegyzés (opcionális)<input v-model="form.megjegyzes" /></label>
      <p v-if="hiba" class="hiba">{{hiba}}</p>
      <button class="btn-primary" @click="ujTetel">Felvásárlás mentése</button>
      <div class="torzsadat-linkek">
        <button type="button" class="link-btn" @click="showPartnerModal=true">Eladók kezelése</button>
        <button type="button" class="link-btn" @click="showZoldsegModal=true">Zöldségek kezelése</button>
        <button type="button" class="link-btn" @click="showRekeszModal=true">Rekesztípusok kezelése</button>
      </div>
    </div>
    <div class="card history-card">
      <div class="section-head"><div><h2>{{datum===today()?'Mai felvásárlások':`${fmtDate(datum)} felvásárlásai`}}</h2><span class="muted">{{csoportositottFelvasarlas.length}} csoport · {{items.length}} tétel</span></div></div>
      <p v-if="betolt">Betöltés…</p>
      <div v-else class="felv-grid">
        <button v-for="group in csoportositottFelvasarlas" :key="group.key" type="button" class="felv-tile" @click="nyitFelvCsoportDetail(group)">
          <div class="felv-tile-photo" :class="{'no-photo':!group.zoldsegKepUrl}" :style="csempeStilus(group.zoldsegId)">
            <div class="felv-tile-overlay">
              <span class="felv-tile-name">{{group.zoldsegNev}}</span>
              <span class="felv-tile-info">{{group.mennyiseg}} {{group.rekeszTipus}} · {{group.egysegar!=null?group.egysegar+' Ft':'ár nélkül'}}</span>
              <span v-if="group.items.length>1" class="felv-tile-info">{{group.items.length}} felvásárlási tétel</span>
            </div>
            <span v-if="!group.zoldsegKepUrl" class="felv-tile-placeholder">🥬</span>
          </div>
        </button>
        <div v-if="!csoportositottFelvasarlas.length" class="ures">Nincs a keresésnek megfelelő felvásárlás.</div>
      </div>
    </div>
  </div>

  <div class="card raktar-card">
    <h2>🏬 Raktáron lévő készlet</h2>
    <p class="muted raktar-hint">Nem naphoz kötött - azonos zöldség + rekesztípus egy kártyán. Áthelyezéskor az eredeti felvásárlási tételt választod ki.</p>
    <div class="felv-grid">
      <div v-for="s in raktar" :key="`${s.zoldsegId}-${s.rekeszTipusId}`" class="raktar-tile-wrap">
        <button type="button" class="felv-tile" @click="nyitRaktarDetail(s)">
          <div class="felv-tile-photo" :class="{'no-photo':!s.zoldsegKepUrl}" :style="csempeStilus(s.zoldsegId)">
            <div class="felv-tile-overlay">
              <span class="felv-tile-name">{{s.zoldsegNev}}</span>
              <span class="felv-tile-info">{{s.maradt}} {{s.rekeszTipus}}</span>
              <span v-if="s.arNelkulMennyiseg>0" class="felv-tile-info">{{s.arNelkulMennyiseg}} db ár nélkül</span>
            </div>
            <span v-if="!s.zoldsegKepUrl" class="felv-tile-placeholder">🥬</span>
          </div>
        </button>
        <button type="button" class="btn-secondary athelyez-btn" @click="nyitAthelyezes(s)">🚚 Áthelyezés a kocsira</button>
      </div>
      <div v-if="!raktar.length" class="ures">Nincs raktáron tárolt áru.</div>
    </div>
  </div>

  <div v-if="raktarDetail" class="overlay" @click.self="zarRaktarDetail">
    <div class="card athelyez-modal raktar-detail-modal">
      <div class="section-head"><h2>{{raktarDetail.zoldsegNev}} — {{raktarDetail.rekeszTipus}}</h2><button type="button" class="icon" @click="zarRaktarDetail">✕</button></div>
      <p class="muted">Összesen: <strong>{{raktarDetail.maradt}} db</strong></p>
      <p class="muted raktar-detail-hint">Válaszd ki, melyik eredeti felvásárlási tételből szeretnél áthelyezni. A rendszer nem von össze és nem választ automatikusan FIFO szerint.</p>
      <div class="forras-list">
        <div v-for="f in raktarDetail.forrasTetelek" :key="f.id" class="forras-row">
          <div>
            <strong>Felvásárlási tétel #{{f.id}}</strong>
            <div class="muted">{{f.maradt}} db · {{f.egysegar!=null?f.egysegar+' Ft/db':'ár nélkül'}}</div>
          </div>
          <button type="button" class="btn-secondary forras-btn" @click="nyitForrasAthelyezes(raktarDetail,f)">Kiválaszt</button>
        </div>
      </div>
    </div>
  </div>

  <div v-if="felvCsoportDetail" class="overlay" @click.self="zarFelvCsoportDetail">
    <div class="card athelyez-modal felv-csoport-modal">
      <div class="section-head"><h2>{{felvCsoportDetail.zoldsegNev}} — {{felvCsoportDetail.rekeszTipus}}</h2><button type="button" class="icon" @click="zarFelvCsoportDetail">✕</button></div>
      <p class="muted">Összesen: <strong>{{felvCsoportDetail.mennyiseg}} db</strong> · {{felvCsoportDetail.egysegar!=null?felvCsoportDetail.egysegar+' Ft/db':'ár nélkül'}}</p>
      <p class="muted raktar-detail-hint">A csempe több eredeti felvásárlási tételt tartalmaz. Az adatbázisban ezek továbbra is külön rekordok maradnak.</p>
      <div class="forras-list">
        <div v-for="it in felvCsoportDetail.items" :key="it.id" class="forras-row">
          <div>
            <strong>#{{it.napiSorszam}}</strong>
            <div class="muted">{{it.mennyiseg}} db · {{it.fizetve?'fizetve':'nincs fizetve'}}<span v-if="it.partnerNev"> · {{it.partnerNev}}</span></div>
          </div>
          <button type="button" class="btn-secondary forras-btn" @click="szerkesztCsoportTetelet(it)">Szerkesztés</button>
        </div>
      </div>
    </div>
  </div>

  <div v-if="felvDetail" class="overlay" @click.self="zarFelvDetail">
    <div class="card athelyez-modal felv-detail-modal">
      <div class="section-head"><h2>{{felvDetail.zoldsegNev}} — #{{felvDetail.napiSorszam}}</h2><button type="button" class="icon" @click="zarFelvDetail">✕</button></div>
      <p class="muted">{{fmtDate(felvDetail.datum)}} · {{new Date(felvDetail.ido).toLocaleTimeString('hu-HU',{hour:'2-digit',minute:'2-digit'})}}</p>
      <label class="checkbox"><input v-model="editForm.sajatTermek" type="checkbox"/> Saját termés</label>
      <label v-if="!editForm.sajatTermek">Eladó<Gyorskereso v-model="editForm.partnerId" :items="partnerek" post-path="/partnerek" mentes-label="Eladó mentése" @created="partnerLetrehozva" /></label>
      <div class="row">
        <label>Zöldség<Gyorskereso v-model="editForm.zoldsegId" :items="zoldsegek" post-path="/zoldsegek" mentes-label="Zöldség mentése" @created="zoldsegLetrehozva" /></label>
        <label>Rekesztípus<Gyorskereso v-model="editForm.rekeszTipusId" :items="rekesztipusok" post-path="/rekesztipusok" mentes-label="Rekesztípus mentése" @created="rekeszLetrehozva" /></label>
      </div>
      <div class="row">
        <label>Mennyiség (db)<input :value="editForm.mennyiseg" @input="editForm.mennyiseg=clampDigits($event.target.value,3)" inputmode="numeric" pattern="[0-9]*" class="narrow-3" /></label>
        <label>Ár (Ft/db)<input :value="editForm.egysegar" @input="editForm.egysegar=clampDigits($event.target.value,5)" inputmode="numeric" pattern="[0-9]*" class="narrow-5" /></label>
      </div>
      <label v-if="!editForm.sajatTermek">Adott üres rekesz (db)<input :value="editForm.adottRekeszDb" @input="editForm.adottRekeszDb=clampDigits($event.target.value,3)" inputmode="numeric" pattern="[0-9]*" class="narrow-3" /></label>
      <div class="row">
        <button type="button" class="toggle-pill" :class="{active:editForm.fizetve}" @click="editForm.fizetve=!editForm.fizetve">{{editForm.fizetve?'✅ Fizetve':'⬜ Fizetve'}}</button>
        <div class="helyszin-radio"><label class="radio-pill"><input v-model="editForm.helyszin" type="radio" value="Kocsi"/> 🚚 Kocsi</label><label class="radio-pill"><input v-model="editForm.helyszin" type="radio" value="Raktar"/> 🏬 Raktár</label></div>
      </div>
      <label>Megjegyzés<input v-model="editForm.megjegyzes" /></label>
      <p v-if="hiba" class="hiba">{{hiba}}</p>
      <div class="nav-row"><button type="button" class="btn-secondary" @click="torolModalbol">🗑️ Törlés</button><button type="button" class="btn-primary" @click="mentesModalbol">Mentés</button></div>
    </div>
  </div>

  <div v-if="athelyezesStock" class="overlay" @click.self="zarAthelyezes">
    <div class="card athelyez-modal">
      <div class="section-head"><h2>Áthelyezés: {{ athelyezesStock.zoldsegNev }} ({{ athelyezesStock.rekeszTipus }})</h2><button type="button" class="icon" @click="zarAthelyezes">✕</button></div>
      <p class="muted">Forrás: felvásárlási tétel #{{athelyezesStock.id}} · Raktáron: {{athelyezesStock.maradt}} db</p>
      <p class="muted">Vételár: {{athelyezesStock.egysegar!=null?athelyezesStock.egysegar+' Ft/db':'nincs megadva'}}</p>
      <div class="row">
        <label>Mennyiség<input v-model="athelyezesForm.mennyiseg" type="number" min="1" :max="athelyezesStock.maradt" /></label>
        <label>Célnap<input v-model="athelyezesForm.celDatum" type="date" /></label>
      </div>
      <p v-if="athelyezesHiba" class="hiba">{{ athelyezesHiba }}</p>
      <div class="nav-row"><button class="btn-secondary" @click="zarAthelyezes">Mégse</button><button class="btn-primary" @click="athelyezes">Áthelyezés</button></div>
    </div>
  </div>

  <TorzsadatModal v-if="showPartnerModal" title="Eladók" api-path="/partnerek" nev-label="Eladó neve" @close="showPartnerModal=false" @changed="torzsadatok"/><ZoldsegModal v-if="showZoldsegModal" @close="showZoldsegModal=false" @changed="torzsadatok"/><TorzsadatModal v-if="showRekeszModal" title="Rekesztípusok" api-path="/rekesztipusok" nev-label="Rekesztípus (pl. M10)" :mutat-megjegyzes="false" @close="showRekeszModal=false" @changed="torzsadatok"/>
</template>
<style scoped>
.toolbar-row{display:flex;gap:10px;align-items:end;margin-bottom:14px}.toolbar-row label{flex:1}.date-picker{max-width:180px}.search{min-width:220px}.grid{display:grid;grid-template-columns:1fr 1.4fr;gap:18px;align-items:start}.card{background:var(--paper);border:1px solid rgba(43,58,46,.12);border-radius:10px;padding:18px}.history-card{overflow:hidden}h2{margin:0 0 12px;font-size:16px;color:var(--chalk-green)}.section-head{display:flex;justify-content:space-between;align-items:center;gap:10px}.wizard-btn{background:var(--chalk-green);color:var(--paper);border:0;border-radius:8px;padding:8px 12px;font-size:12px;font-weight:700;cursor:pointer;white-space:nowrap}.row{display:grid;grid-template-columns:1fr 1fr;gap:10px}label{display:flex;flex-direction:column;gap:5px;font-size:12px;font-weight:600;color:var(--olive);text-transform:uppercase;letter-spacing:.03em;margin-bottom:10px}label.checkbox{flex-direction:row;align-items:center;gap:8px}.small{font-size:10px}.with-icon{display:flex;gap:6px}.with-icon select{flex:1}.price-input{max-width:120px!important}.table-wrap{overflow-x:auto}table{font-size:12px;min-width:820px}.thumb{width:34px;height:34px;object-fit:cover;border-radius:6px;display:block}.muted,.ures{color:var(--olive)}.ures{text-align:center;padding:16px}.hiba{color:#b3441e;font-size:13px}@media(max-width:900px){.grid{grid-template-columns:1fr}}@media(max-width:650px){.toolbar-row{flex-direction:column;align-items:stretch}.date-picker,.search{max-width:none;min-width:0}.date-picker input{width:100%;box-sizing:border-box}.row{grid-template-columns:1fr}.section-head{flex-direction:column;align-items:stretch}.card input,.card select{box-sizing:border-box;max-width:100%}.felv-detail-modal,.felv-csoport-modal,.athelyez-modal{max-width:100%}}
.helyszin-radio{display:flex;gap:8px}.radio-pill{flex:1;flex-direction:row!important;align-items:center;justify-content:center;gap:6px;border:1px solid rgba(43,58,46,.2);border-radius:20px;padding:8px 10px;cursor:pointer;text-transform:none!important;font-weight:600;color:var(--chalk-green);margin-bottom:0!important}.radio-pill input{margin:0}
.raktar-card{margin-top:18px}.raktar-hint{margin:-6px 0 12px}.raktar-tile-wrap{display:flex;flex-direction:column}.athelyez-btn{margin-top:6px;font-size:12px;padding:7px 10px;border-radius:8px;background:#fff;border:1px solid rgba(43,58,46,.2);color:var(--chalk-green);cursor:pointer}
.felv-grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(160px,1fr));gap:12px}.felv-tile{text-align:left;border:1px solid rgba(43,58,46,.15);border-radius:12px;background:#fff;overflow:hidden;cursor:pointer;padding:0;display:flex;flex-direction:column;box-shadow:0 2px 7px rgba(0,0,0,.05)}.felv-tile-photo{position:relative;aspect-ratio:16/9;background-color:rgba(107,122,79,.08);background-size:cover;background-position:center;display:flex;align-items:flex-end;justify-content:center}.felv-tile-placeholder{font-size:30px;margin-bottom:10px}.felv-tile-overlay{position:absolute;top:0;left:0;right:0;background:rgba(255,255,255,.92);display:flex;flex-direction:column}.felv-tile-name{padding:6px 9px 1px;font-size:13px;font-weight:700;color:var(--chalk-green);text-transform:none}.felv-tile-info{padding:0 9px 6px;font-size:11px;font-weight:600;color:var(--olive);text-transform:none}
.felv-detail-modal,.felv-csoport-modal{max-width:460px}.overlay{position:fixed;inset:0;background:rgba(35,38,32,.55);display:flex;align-items:center;justify-content:center;z-index:60;padding:14px}.athelyez-modal{width:100%;max-width:380px}.nav-row{display:flex;gap:10px;margin-top:10px}.nav-row button{flex:1;min-height:40px;border-radius:8px;font-weight:700;cursor:pointer}.btn-primary{background:var(--chalk-green);color:var(--paper);border:0}.btn-secondary{background:#fff;border:1px solid rgba(43,58,46,.2);color:var(--chalk-green)}.narrow-3{max-width:70px}.narrow-5{max-width:95px}.foto-row{margin:-2px 0 14px}.foto-btn{width:100%;min-height:44px;border-radius:10px;border:2px dashed rgba(43,58,46,.3);background:rgba(107,122,79,.05);color:var(--chalk-green);font-weight:700;font-size:13px;cursor:pointer;text-transform:none}.foto-btn:hover{border-color:var(--olive)}.foto-btn:disabled{opacity:.6;cursor:default}
.toggle-pill{display:inline-flex;align-items:center;gap:6px;border:1px solid rgba(43,58,46,.2);border-radius:20px;padding:9px 16px;background:#fff;color:var(--chalk-green);font-weight:700;font-size:13px;cursor:pointer;margin-bottom:14px}.toggle-pill.active{background:var(--chalk-green);color:var(--paper);border-color:var(--chalk-green)}.torzsadat-linkek{display:flex;flex-wrap:wrap;gap:8px;margin-top:14px;padding-top:12px;border-top:1px solid rgba(43,58,46,.1)}.link-btn{background:#fff;border:1px solid rgba(43,58,46,.25);border-radius:8px;padding:8px 12px;font-size:12px;font-weight:700;color:var(--chalk-green);cursor:pointer;text-transform:none}.link-btn:hover{background:rgba(107,122,79,.08);border-color:var(--olive)}.raktar-detail-modal{max-width:480px}.raktar-detail-hint{font-size:12px;line-height:1.45}.forras-list{display:flex;flex-direction:column;gap:8px;margin-top:12px}.forras-row{display:flex;justify-content:space-between;align-items:center;gap:12px;border:1px solid rgba(43,58,46,.12);border-radius:9px;padding:10px;background:#fff}.forras-row strong{color:var(--chalk-green);font-size:13px}.forras-row .muted{font-size:11px;margin-top:3px}.forras-btn{white-space:nowrap;padding:7px 10px;border-radius:8px;font-weight:700;cursor:pointer}
</style>
