<script setup>
import { ref, onMounted } from 'vue'
import client from '../api/client'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const isAdmin = auth.role === 'Admin'

const miTartozunk = ref([])
const nekunkTartoznak = ref([])
const veszteseg = ref([])
const konyveles = ref(null)
const rekeszReszletezo = ref([])
const rrDatum = ref(new Date().toISOString().slice(0, 10))
const betolt = ref(true)
const hiba = ref('')

const ma = new Date().toISOString().slice(0, 10)
const honapEleje = ma.slice(0, 8) + '01'
const tol = ref(honapEleje)
const ig = ref(ma)

async function frissitKonyveles() {
  if (!isAdmin) return
  hiba.value = ''
  try {
    konyveles.value = (await client.get('/riportok/konyveles', { params: { tol: tol.value, ig: ig.value } })).data
  } catch (e) {
    hiba.value = 'Nem sikerült betölteni a könyvelést.'
  }
}

async function frissitRekeszReszletezo() {
  if (!isAdmin) return
  try {
    rekeszReszletezo.value = (await client.get('/riportok/rekeszreszletezo', { params: { datum: rrDatum.value } })).data
  } catch (e) {
    hiba.value = 'Nem sikerült betölteni a rekesz-részletezőt.'
  }
}

async function frissit() {
  betolt.value = true
  hiba.value = ''
  try {
    const [a, b, c] = await Promise.all([
      client.get('/riportok/mi-tartozunk'),
      client.get('/riportok/nekunk-tartoznak'),
      client.get('/riportok/rekeszveszteseg')
    ])
    miTartozunk.value = a.data
    nekunkTartoznak.value = b.data
    veszteseg.value = c.data
    await frissitKonyveles()
    await frissitRekeszReszletezo()
  } catch (e) {
    hiba.value = 'Nem sikerült betölteni a riportot.'
  } finally {
    betolt.value = false
  }
}
onMounted(frissit)

function ft(n) {
  return new Intl.NumberFormat('hu-HU').format(Math.round(n || 0)) + ' Ft'
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
        <div class="stat accent">
          <div class="val">{{ ft(konyveles.bevetel) }}</div>
          <div class="lbl">Bevétel</div>
        </div>
        <div class="stat">
          <div class="val">{{ ft(konyveles.kiadasOsszesen) }}</div>
          <div class="lbl">Kiadás összesen</div>
        </div>
        <div class="stat olive">
          <div class="val">{{ ft(konyveles.nyereseg) }}</div>
          <div class="lbl">Nyereség</div>
        </div>
      </div>

      <div v-if="konyveles" class="reszletek">
        <div>Ebből felvásárolt áru: <b>{{ ft(konyveles.kiadasVasarolt) }}</b></div>
        <div>Ebből saját áru becsült önköltsége: <b>{{ ft(konyveles.kiadasSajatBecsult) }}</b></div>
        <div>Tételszám: {{ konyveles.felvasarlasTetelSzam }} felvásárlás, {{ konyveles.eladasTetelSzam }} eladás</div>
        <div v-if="konyveles.felvasarlasArNelkul || konyveles.eladasArNelkul" class="figyelmeztetes">
          ⚠ {{ konyveles.felvasarlasArNelkul }} felvásárlási és {{ konyveles.eladasArNelkul }} eladási tételen nincs
          megadva egységár — ezek nem szerepelnek a fenti összegekben.
        </div>
      </div>

      <table v-if="konyveles?.zoldsegenkent?.length" class="zoldseg-tabla">
        <thead><tr><th>Zöldség</th><th class="num">Eladott db</th><th class="num">Bevétel</th></tr></thead>
        <tbody>
          <tr v-for="z in konyveles.zoldsegenkent" :key="z.zoldseg">
            <td>{{ z.zoldseg }}</td><td class="num">{{ z.mennyiseg }}</td><td class="num">{{ ft(z.bevetel) }}</td>
          </tr>
        </tbody>
      </table>
    </div>

    <div class="card rekesz-card">
      <div class="konyveles-head">
        <h2>Rekesz mennyiség részletező</h2>
        <label class="rr-datum">Nap <input v-model="rrDatum" type="date" @change="frissitRekeszReszletezo" /></label>
      </div>
      <p class="muted rr-hint">Kocsira került (saját + felvásárolt + áthozott) / aznap visszahozott, rekesztípusonként.</p>
      <div class="rr-grid">
        <div v-for="r in rekeszReszletezo" :key="r.rekeszTipusId" class="rr-tile">
          <span class="rr-tipus">{{ r.rekeszTipus }} rekesz</span>
          <span class="rr-szamok">{{ r.osszesen }}/{{ r.visszahozott }}</span>
        </div>
        <div v-if="!rekeszReszletezo.length" class="ures">Erre a napra nincs adat.</div>
      </div>
    </div>

    <div class="grid">
      <div class="card">
        <h2>Mi tartozunk (eladóknak)</h2>
        <table>
          <thead><tr><th>Eladó</th><th>Rekesz</th><th class="num">Db</th></tr></thead>
          <tbody>
            <tr v-for="(r, i) in miTartozunk" :key="i">
              <td>{{ r.partnerNev }}</td><td>{{ r.rekeszTipus }}</td>
              <td class="num owe">{{ r.mennyiseg }}</td>
            </tr>
            <tr v-if="!miTartozunk.length"><td colspan="3" class="ures">Nincs nyitott tartozás.</td></tr>
          </tbody>
        </table>
      </div>

      <div class="card">
        <h2>Nekünk tartoznak (vevők)</h2>
        <table>
          <thead><tr><th>Vevő</th><th>Rekesz</th><th class="num">Db</th></tr></thead>
          <tbody>
            <tr v-for="(r, i) in nekunkTartoznak" :key="i">
              <td>{{ r.vevoNev || '(névtelen)' }}</td><td>{{ r.rekeszTipus }}</td>
              <td class="num owe">{{ r.mennyiseg }}</td>
            </tr>
            <tr v-if="!nekunkTartoznak.length"><td colspan="3" class="ures">Nincs nyitott tartozás.</td></tr>
          </tbody>
        </table>
      </div>

      <div class="card wide">
        <h2>Kifizetett rekeszveszteség</h2>
        <div class="table-wrap">
        <table>
          <thead><tr><th>#</th><th>Dátum</th><th>Vevő</th><th>Zöldség</th><th>Rekesz</th><th class="num">Hiányzó db</th><th>Megjegyzés</th></tr></thead>
          <tbody>
            <tr v-for="v in veszteseg" :key="v.id">
              <td>#{{ v.napiSorszam }}</td><td>{{ v.datum }}</td>
              <td>{{ v.vevoNev || '(névtelen)' }}</td><td>{{ v.zoldsegNev }}</td><td>{{ v.rekeszTipus }}</td>
              <td class="num owe">{{ v.hianyzoDb }}</td><td>{{ v.megjegyzes }}</td>
            </tr>
            <tr v-if="!veszteseg.length"><td colspan="7" class="ures">Nincs elkönyvelt rekeszveszteség.</td></tr>
          </tbody>
        </table>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.konyveles-card { margin-bottom: 18px; }
.rekesz-card { margin-bottom: 18px; }
.rr-datum { display: flex; flex-direction: column; gap: 3px; font-size: 11px; font-weight: 600; color: var(--olive); text-transform: uppercase; }
.rr-datum input { padding: 6px 8px; }
.rr-hint { margin: -6px 0 12px; }
.rr-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(150px, 1fr)); gap: 10px; }
.rr-tile { background: var(--chalk-green); color: var(--paper); border-radius: 10px; padding: 12px 14px; display: flex; flex-direction: column; gap: 4px; }
.rr-tipus { font-size: 11px; text-transform: uppercase; letter-spacing: 0.05em; opacity: 0.75; }
.rr-szamok { font-family: monospace; font-size: 20px; font-weight: 700; }
.konyveles-head { display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 10px; margin-bottom: 14px; }
.datumsav { display: flex; gap: 12px; }
.datumsav label { display: flex; flex-direction: column; gap: 3px; font-size: 11px; font-weight: 600; color: var(--olive); text-transform: uppercase; }
.datumsav input { padding: 6px 8px; }
.stat-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 10px; margin-bottom: 14px; }
.stat { background: var(--chalk-green); color: var(--paper); border-radius: 10px; padding: 14px 12px; text-align: center; }
.stat .val { font-family: monospace; font-size: 19px; font-weight: 700; }
.stat .lbl { font-size: 10px; text-transform: uppercase; letter-spacing: 0.06em; opacity: 0.7; margin-top: 3px; }
.stat.accent { background: var(--kapia); }
.stat.olive { background: var(--olive); }
.reszletek { font-size: 13px; line-height: 1.7; margin-bottom: 12px; }
.figyelmeztetes { color: var(--owe); font-weight: 600; }
.zoldseg-tabla { font-size: 12.5px; }
.table-wrap { overflow-x: auto; }
.table-wrap table { min-width: 560px; }
.grid { display: grid; grid-template-columns: 1fr 1fr; gap: 18px; }
.card.wide { grid-column: 1 / -1; }
@media (max-width: 800px) { .grid, .stat-grid { grid-template-columns: 1fr; } }
@media (max-width: 480px) {
  .konyveles-head { flex-direction: column; align-items: stretch; }
  .datumsav { justify-content: space-between; }
  .datumsav label { flex: 1; }
}
h2 { margin-top: 0; font-size: 16px; color: var(--chalk-green); }
.num { text-align: right; font-family: monospace; }
.num.owe { color: var(--owe); font-weight: 700; }
.ures { color: var(--olive); text-align: center; padding: 16px; }
</style>
