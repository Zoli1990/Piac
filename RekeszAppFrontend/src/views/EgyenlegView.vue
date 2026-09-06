<script setup>
import { computed, onMounted, ref } from 'vue'
import client from '../api/client'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const isAdmin = auth.role === 'Admin'

const miTartozunk = ref([])
const nekunkTartoznak = ref([])
const veszteseg = ref([])
const konyveles = ref(null)
const rekeszReszletezo = ref([])
const betolt = ref(true)
const hiba = ref('')

const modalNyitva = ref(false)
const modalIrany = ref('elado')
const modalPartner = ref(null)
const mentesFolyamatban = ref(false)

const ma = new Date().toISOString().slice(0, 10)
const honapEleje = ma.slice(0, 8) + '01'
const tol = ref(honapEleje)
const ig = ref(ma)
const rrDatum = ref(ma)

async function frissitKonyveles() {
  if (!isAdmin) return
  try {
    konyveles.value = (await client.get('/riportok/konyveles', { params: { tol: tol.value, ig: ig.value } })).data
  } catch {
    hiba.value = 'Nem sikerült betölteni a könyvelést.'
  }
}

async function frissitRekeszReszletezo() {
  if (!isAdmin) return
  try {
    rekeszReszletezo.value = (await client.get('/riportok/rekeszreszletezo', { params: { datum: rrDatum.value } })).data
  } catch {
    hiba.value = 'Nem sikerült betölteni a rekesz-részletezőt.'
  }
}

async function frissitEgyenleg() {
  const adat = (await client.get('/egyenleg')).data
  miTartozunk.value = adat.eladok || []
  nekunkTartoznak.value = adat.vevok || []
}

async function frissit() {
  betolt.value = true
  hiba.value = ''
  try {
    const [, , c] = await Promise.all([
      frissitEgyenleg(),
      frissitKonyveles(),
      client.get('/riportok/rekeszveszteseg')
    ])
    veszteseg.value = c.data
    await frissitRekeszReszletezo()
  } catch {
    hiba.value = 'Nem sikerült betölteni a riportot.'
  } finally {
    betolt.value = false
  }
}

onMounted(frissit)

function ft(n) {
  return new Intl.NumberFormat('hu-HU').format(Math.round(Number(n) || 0)) + ' Ft'
}

function tartozasFt(tetel) {
  if (tetel.egysegar == null || tetel.osszeg == null) return '—'
  return ft(tetel.osszeg)
}

function osszegMegjelenit(osszeg) {
  return Number(osszeg) > 0 ? ft(osszeg) : '—'
}

function partnerNev(sor, irany) {
  return irany === 'elado' ? sor.partnerNev : (sor.vevoNev || '(névtelen)')
}

const eladok = computed(() => miTartozunk.value)
const vevok = computed(() => nekunkTartoznak.value)

function partnerCsoportok(sorok, irany) {
  const map = new Map()
  for (const sor of sorok) {
    const id = irany === 'elado' ? sor.partnerId : sor.vevoId
    const key = id == null ? `null-${partnerNev(sor, irany)}` : String(id)
    if (!map.has(key)) {
      map.set(key, {
        id,
        nev: partnerNev(sor, irany),
        sorok: [],
        osszeg: 0,
        rekeszDb: 0
      })
    }
    const p = map.get(key)
    p.sorok.push(sor)
    p.osszeg += Number(sor.osszeg) || 0
    p.rekeszDb += Number(sor.rekeszDb) || 0
  }
  return [...map.values()]
}

const eladoCsoportok = computed(() => partnerCsoportok(eladok.value, 'elado'))
const vevoCsoportok = computed(() => partnerCsoportok(vevok.value, 'vevo'))

function nyitModal(partner, irany) {
  modalIrany.value = irany
  modalPartner.value = {
    ...partner,
    sorok: partner.sorok.map(sor => ({
      ...sor,
      tetelek: sor.tetelek.map(t => ({
        ...t,
        hozottDbInput: t.hozottDb,
        teljesMennyisegInput: false,
        fizetveInput: t.fizetve
      }))
    }))
  }
  modalNyitva.value = true
}

function bezarModal() {
  if (!mentesFolyamatban.value) modalNyitva.value = false
}

function tetelTeljesMennyiseg(tetel) {
  tetel.hozottDbInput = tetel.teljesMennyiseg
}

async function mentesTetel(sor, tetel) {
  if (mentesFolyamatban.value) return
  mentesFolyamatban.value = true
  hiba.value = ''
  try {
    const irany = modalIrany.value
    const hozott = tetel.teljesMennyisegInput ? tetel.teljesMennyiseg : Number(tetel.hozottDbInput) || 0
    const url = irany === 'elado' ? `/egyenleg/elado/${tetel.id}` : `/egyenleg/vevo/${tetel.id}`
    await client.patch(url, {
      hozottDb: hozott,
      teljesMennyiseg: tetel.teljesMennyisegInput,
      fizetve: tetel.fizetveInput
    })
    await frissitEgyenleg()
    const friss = (irany === 'elado' ? eladoCsoportok.value : vevoCsoportok.value)
      .find(x => String(x.id) === String(modalPartner.value.id))
    if (friss) {
      modalPartner.value = {
        ...friss,
        sorok: friss.sorok.map(x => ({
          ...x,
          tetelek: x.tetelek.map(t => ({
            ...t,
            hozottDbInput: t.hozottDb,
            teljesMennyisegInput: false,
            fizetveInput: t.fizetve
          }))
        }))
      }
    } else {
      modalNyitva.value = false
    }
  } catch (e) {
    hiba.value = e.response?.data?.message || 'A tétel mentése sikertelen.'
  } finally {
    mentesFolyamatban.value = false
  }
}

function modalTartozasOsszeg() {
  if (!modalPartner.value) return 0
  return modalPartner.value.sorok.reduce((sum, s) => sum + (Number(s.osszeg) || 0), 0)
}

function modalRekeszOsszegzes() {
  if (!modalPartner.value) return []
  const map = new Map()
  for (const s of modalPartner.value.sorok) {
    if (!map.has(s.rekeszTipusId)) map.set(s.rekeszTipusId, { rekeszTipus: s.rekeszTipus, db: 0, osszeg: 0 })
    const x = map.get(s.rekeszTipusId)
    x.db += Number(s.rekeszDb) || 0
    x.osszeg += Number(s.osszeg) || 0
  }
  return [...map.values()]
}

function modalTetelSorok() {
  if (!modalPartner.value) return []
  return modalPartner.value.sorok.flatMap(s => s.tetelek.map(t => ({ ...t, rekeszTipus: s.rekeszTipus })))
}

function modalSorAdatok() {
  return modalTetelSorok()
}

function modalCsoportOsszegzes() {
  return modalRekeszOsszegzes()
}

function modalCsoportok() {
  if (!modalPartner.value) return []
  return modalPartner.value.sorok
}

function modalOsszesDb() {
  return modalCsoportok().reduce((sum, s) => sum + (Number(s.rekeszDb) || 0), 0)
}
</script>

<template>
  <p v-if="hiba" class="hiba">{{ hiba }}</p>
  <p v-if="betolt">Betöltés…</p>

  <div v-else>
    <div v-if="isAdmin" class="card konyveles-card">
      <div class="konyveles-head">
        <h2>Könyvelés</h2>
        <div class="datumsav">
          <label>Tól <input v-model="tol" type="date" @change="frissitKonyveles" /></label>
          <label>Ig <input v-model="ig" type="date" @change="frissitKonyveles" /></label>
        </div>
      </div>
      <div v-if="konyveles" class="stat-grid">
        <div class="stat accent"><div class="val">{{ ft(konyveles.bevetel) }}</div><div class="lbl">Bevétel</div></div>
        <div class="stat"><div class="val">{{ ft(konyveles.kiadasOsszesen) }}</div><div class="lbl">Kiadás összesen</div></div>
        <div class="stat olive"><div class="val">{{ ft(konyveles.nyereseg) }}</div><div class="lbl">Nyereség</div></div>
      </div>
      <div v-if="konyveles" class="reszletek">
        <div>Ebből felvásárolt áru: <b>{{ ft(konyveles.kiadasVasarolt) }}</b></div>
        <div>Ebből saját áru becsült önköltsége: <b>{{ ft(konyveles.kiadasSajatBecsult) }}</b></div>
        <div>Tételszám: {{ konyveles.felvasarlasTetelSzam }} felvásárlás, {{ konyveles.eladasTetelSzam }} eladás</div>
        <div v-if="konyveles.felvasarlasArNelkul || konyveles.eladasArNelkul" class="figyelmeztetes">
          ⚠ {{ konyveles.felvasarlasArNelkul }} felvásárlási és {{ konyveles.eladasArNelkul }} eladási tételen nincs megadva egységár — ezek nem szerepelnek a fenti összegekben.
        </div>
      </div>
      <table v-if="konyveles?.zoldsegenkent?.length" class="zoldseg-tabla">
        <thead><tr><th>Zöldség</th><th class="num">Eladott db</th><th class="num">Bevétel</th></tr></thead>
        <tbody><tr v-for="z in konyveles.zoldsegenkent" :key="z.zoldseg"><td>{{ z.zoldseg }}</td><td class="num">{{ z.mennyiseg }}</td><td class="num">{{ ft(z.bevetel) }}</td></tr></tbody>
      </table>
    </div>

    <div class="card rekesz-card">
      <div class="konyveles-head">
        <h2>Rekesz mennyiség részletező</h2>
        <label class="rr-datum">Nap <input v-model="rrDatum" type="date" @change="frissitRekeszReszletezo" /></label>
      </div>
      <p class="muted rr-hint">Kocsira került (saját + felvásárolt + áthozott) / aznap visszahozott, rekesztípusonként.</p>
      <div class="rr-grid">
        <div v-for="r in rekeszReszletezo" :key="r.rekeszTipusId" class="rr-tile"><span class="rr-tipus">{{ r.rekeszTipus }} rekesz</span><span class="rr-szamok">{{ r.osszesen }}/{{ r.visszahozott }}</span></div>
        <div v-if="!rekeszReszletezo.length" class="ures">Erre a napra nincs adat.</div>
      </div>
    </div>

    <div class="egyenleg-grid">
      <section class="card egyenleg-card">
        <h2>Mi tartozunk (eladóknak)</h2>
        <div v-if="!eladoCsoportok.length" class="ures">Nincs nyitott tartozás.</div>
        <div v-for="p in eladoCsoportok" :key="`e-${p.id}`" class="partner-block" @click="nyitModal(p, 'elado')">
          <div class="partner-name">{{ p.nev }}</div>
          <div v-for="s in p.sorok" :key="s.rekeszTipusId" class="balance-row">
            <span class="crate">{{ s.rekeszTipus }}</span>
            <span class="db">{{ s.rekeszDb }} db</span>
            <span class="money">{{ osszegMegjelenit(s.osszeg) }}</span>
          </div>
          <div class="partner-total" v-if="p.osszeg">Tartozás: <b>{{ ft(p.osszeg) }}</b></div>
        </div>
      </section>

      <section class="card egyenleg-card">
        <h2>Nekünk tartoznak (vevők)</h2>
        <div v-if="!vevoCsoportok.length" class="ures">Nincs nyitott tartozás.</div>
        <div v-for="p in vevoCsoportok" :key="`v-${p.id}`" class="partner-block" @click="nyitModal(p, 'vevo')">
          <div class="partner-name">{{ p.nev }}</div>
          <div v-for="s in p.sorok" :key="s.rekeszTipusId" class="balance-row">
            <span class="crate">{{ s.rekeszTipus }}</span>
            <span class="db">{{ s.rekeszDb }} db</span>
            <span class="money">{{ osszegMegjelenit(s.osszeg) }}</span>
          </div>
          <div class="partner-total" v-if="p.osszeg">Tartozás: <b>{{ ft(p.osszeg) }}</b></div>
        </div>
      </section>

      <div class="card wide">
        <h2>Kifizetett rekeszveszteség</h2>
        <div class="table-wrap">
          <table>
            <thead><tr><th>#</th><th>Dátum</th><th>Vevő</th><th>Zöldség</th><th>Rekesz</th><th class="num">Hiányzó db</th><th>Megjegyzés</th></tr></thead>
            <tbody>
              <tr v-for="v in veszteseg" :key="v.id"><td>#{{ v.napiSorszam }}</td><td>{{ v.datum }}</td><td>{{ v.vevoNev || '(névtelen)' }}</td><td>{{ v.zoldsegNev }}</td><td>{{ v.rekeszTipus }}</td><td class="num owe">{{ v.hianyzoDb }}</td><td>{{ v.megjegyzes }}</td></tr>
              <tr v-if="!veszteseg.length"><td colspan="7" class="ures">Nincs elkönyvelt rekeszveszteség.</td></tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>

  <div v-if="modalNyitva && modalPartner" class="overlay" @click.self="bezarModal">
    <div class="modal egyenleg-modal">
      <div class="modal-head">
        <div><h3>{{ modalPartner.nev }}</h3><div class="modal-sub">{{ modalIrany === 'elado' ? 'Mi tartozunk' : 'Nekünk tartoznak' }}</div></div>
        <button class="close" @click="bezarModal">✕</button>
      </div>

      <div class="tetel-lista">
        <div v-for="s in modalCsoportok()" :key="`${s.rekeszTipusId}`" class="modal-crate-group">
          <div class="modal-crate-title">{{ s.rekeszTipus }}</div>
          <div v-for="t in s.tetelek" :key="t.id" class="tetel-card">
            <div class="tetel-top">
              <strong>{{ t.zoldsegNev }}</strong>
              <span>{{ t.mennyiseg }} db</span>
              <b>{{ tartozasFt(t) }}</b>
            </div>
            <div class="tetel-controls">
              <label>Hozott db <input v-model.number="t.hozottDbInput" type="number" min="0" :max="t.teljesMennyiseg" /></label>
              <label class="check"><input v-model="t.teljesMennyisegInput" type="checkbox" @change="tetelTeljesMennyiseg(t)" /> Teljes mennyiség</label>
              <label class="check"><input v-model="t.fizetveInput" type="checkbox" /> Fizetve</label>
              <button class="save-small" type="button" :disabled="mentesFolyamatban" @click="mentesTetel(s, t)">Mentés</button>
            </div>
          </div>
        </div>
      </div>

      <div class="modal-summary">
        <div><b>Tartozás összesen:</b></div>
        <div v-for="s in modalCsoportOsszegzes()" :key="s.rekeszTipus" class="summary-row">
          <span>{{ s.rekeszTipus }}</span><span>{{ s.db }} db</span><span>{{ osszegMegjelenit(s.osszeg) }}</span>
        </div>
        <div class="summary-total"><span>Összesen</span><span>{{ modalOsszesDb() }} db</span><strong>{{ ft(modalTartozasOsszeg()) }}</strong></div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.konyveles-card,.rekesz-card{margin-bottom:18px}.konyveles-head{display:flex;justify-content:space-between;align-items:center;flex-wrap:wrap;gap:10px;margin-bottom:14px}.datumsav{display:flex;gap:12px}.datumsav label,.rr-datum{display:flex;flex-direction:column;gap:3px;font-size:11px;font-weight:600;color:var(--olive);text-transform:uppercase}.datumsav input,.rr-datum input{padding:6px 8px}.stat-grid{display:grid;grid-template-columns:repeat(3,1fr);gap:10px;margin-bottom:14px}.stat{background:var(--chalk-green);color:var(--paper);border-radius:10px;padding:14px 12px;text-align:center}.stat .val{font-family:monospace;font-size:19px;font-weight:700}.stat .lbl{font-size:10px;text-transform:uppercase;letter-spacing:.06em;opacity:.7;margin-top:3px}.stat.accent{background:var(--kapia)}.stat.olive{background:var(--olive)}.reszletek{font-size:13px;line-height:1.7;margin-bottom:12px}.figyelmeztetes{color:var(--owe);font-weight:600}.zoldseg-tabla{font-size:12.5px}.rr-hint{margin:-6px 0 12px}.rr-grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(150px,1fr));gap:10px}.rr-tile{background:var(--chalk-green);color:var(--paper);border-radius:10px;padding:12px 14px;display:flex;flex-direction:column;gap:4px}.rr-tipus{font-size:11px;text-transform:uppercase;letter-spacing:.05em;opacity:.75}.rr-szamok{font-family:monospace;font-size:20px;font-weight:700}
.egyenleg-grid{display:grid;grid-template-columns:1fr 1fr;gap:18px}.wide{grid-column:1/-1}.egyenleg-card h2{margin-bottom:12px}.partner-block{border:1px solid rgba(43,58,46,.13);border-radius:10px;padding:10px 12px;margin-bottom:9px;cursor:pointer;background:rgba(255,255,255,.28)}.partner-block:hover{background:rgba(255,255,255,.5)}.partner-name{font-weight:800;color:var(--chalk-green);font-size:15px;margin-bottom:6px}.balance-row{display:grid;grid-template-columns:1fr auto auto;gap:12px;align-items:center;padding:4px 0;font-family:monospace}.crate{font-weight:700}.db{white-space:nowrap}.money{font-weight:800;text-align:right;color:var(--owe);white-space:nowrap}.partner-total{border-top:1px solid rgba(43,58,46,.12);margin-top:6px;padding-top:6px;text-align:right;font-size:12px;color:var(--olive)}
.num{text-align:right;font-family:monospace}.owe{color:var(--owe);font-weight:700}.table-wrap{overflow-x:auto}.table-wrap table{min-width:560px}.ures{color:var(--olive);text-align:center;padding:16px}.hiba{color:var(--owe)}
.overlay{position:fixed;inset:0;background:rgba(35,38,32,.55);display:flex;align-items:center;justify-content:center;z-index:100;padding:14px}.modal{background:var(--paper,#F7F3E8);border-radius:12px;width:100%;max-width:720px;max-height:90vh;overflow:auto;padding:18px}.modal-head{display:flex;justify-content:space-between;align-items:flex-start;gap:12px;margin-bottom:14px}.modal-head h3{margin:0;color:var(--chalk-green);font-size:19px}.modal-sub{font-size:11px;color:var(--olive);text-transform:uppercase;letter-spacing:.05em;margin-top:3px}.close{border:0;background:none;font-size:18px;color:var(--olive);cursor:pointer}.modal-crate-group{margin-bottom:16px}.modal-crate-title{font-weight:800;color:var(--chalk-green);border-bottom:2px solid rgba(43,58,46,.12);padding-bottom:5px;margin-bottom:7px}.tetel-card{border:1px solid rgba(43,58,46,.14);border-radius:10px;padding:10px;margin-bottom:7px;background:rgba(255,255,255,.35)}.tetel-top{display:grid;grid-template-columns:1fr auto auto;gap:12px;align-items:center}.tetel-top b{color:var(--owe);white-space:nowrap}.tetel-controls{display:flex;align-items:center;gap:10px;flex-wrap:wrap;margin-top:8px;font-size:12px}.tetel-controls label:first-child{display:flex;align-items:center;gap:5px}.tetel-controls input[type=number]{width:68px;padding:5px}.check{display:flex;align-items:center;gap:4px;white-space:nowrap}.save-small{border:0;border-radius:7px;padding:6px 10px;background:var(--chalk-green);color:var(--paper);cursor:pointer;font-weight:700}.save-small:disabled{opacity:.55}.modal-summary{border-top:2px solid rgba(43,58,46,.15);padding-top:12px;margin-top:8px}.summary-row{display:grid;grid-template-columns:1fr auto auto;gap:14px;font-family:monospace;padding:3px 0}.summary-row span:last-child{text-align:right;color:var(--owe)}.summary-total{display:grid;grid-template-columns:1fr auto auto;gap:14px;border-top:1px solid rgba(43,58,46,.12);margin-top:6px;padding-top:7px;font-family:monospace}.summary-total strong{color:var(--owe)}
@media(max-width:800px){.egyenleg-grid,.stat-grid{grid-template-columns:1fr}}@media(max-width:560px){.datumsav{width:100%}.datumsav label{flex:1}.tetel-top{grid-template-columns:1fr auto}.tetel-top b{grid-column:1/-1}.tetel-controls{align-items:flex-start}.balance-row{grid-template-columns:1fr auto}.money{grid-column:2}.modal{padding:14px}.summary-row,.summary-total{grid-template-columns:1fr auto auto;gap:8px;font-size:12px}}
</style>
