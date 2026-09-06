<script setup>
import { ref, onMounted } from 'vue'
import client from '../api/client'

const emit = defineEmits(['close'])
const sorok = ref([])
const hiba = ref('')
const betolt = ref(true)

function ar(s){
  return s.atlagVetelAr != null ? `${Number(s.atlagVetelAr).toFixed(2)} Ft/db` : 'ár nélkül'
}

onMounted(async () => {
  try {
    sorok.value = (await client.get('/riportok/keszlet')).data
  } catch (e) {
    hiba.value = 'Nem sikerült betölteni a kocsi tartalmát.'
  } finally {
    betolt.value = false
  }
})
</script>

<template>
  <div class="overlay" @click.self="emit('close')">
    <div class="modal">
      <div class="modal-head">
        <h3>Kocsin lévő áru (ma)</h3>
        <button class="close" @click="emit('close')">✕</button>
      </div>
      <p v-if="hiba" class="hiba">{{ hiba }}</p>
      <p v-if="betolt">Betöltés…</p>
      <table v-else>
        <thead><tr><th>Zöldség</th><th>Rekesz</th><th class="num">Felvéve</th><th class="num">Eladva</th><th class="num">Maradt</th><th class="num">Átlag vételár</th></tr></thead>
        <tbody>
          <tr v-for="(s, i) in sorok" :key="i" :class="{ elfogyott: s.kocsinMaradt <= 0 }">
            <td>{{ s.zoldsegNev }}</td>
            <td>{{ s.rekeszTipus }}</td>
            <td class="num">{{ s.felvasarolva }}</td>
            <td class="num">{{ s.eladva }}</td>
            <td class="num maradt">{{ s.kocsinMaradt }}</td>
            <td class="num">{{ ar(s) }}<span v-if="s.arNelkulMennyiseg > 0" class="ar-nelkul"> · {{s.arNelkulMennyiseg}} db ár nélkül</span></td>
          </tr>
          <tr v-if="!sorok.length"><td colspan="6" class="ures">Ma még nincs felvett áru.</td></tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.overlay { position: fixed; inset: 0; background: rgba(35,38,32,0.5); display: flex; align-items: center; justify-content: center; z-index: 50; padding: 16px; }
.modal { background: #F7F3E8; border-radius: 10px; padding: 18px 20px; width: 100%; max-width: 760px; max-height: 80vh; overflow-y: auto; }
.modal-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px; }
.modal-head h3 { margin: 0; font-size: 16px; color: #2B3A2E; }
.close { background: none; border: none; font-size: 16px; cursor: pointer; color: #6B7A4F; }
.hiba { color: #B3441E; font-size: 13px; margin: 4px 0; }
table { font-size: 13px; }
.num { text-align: right; font-family: monospace; }
.maradt { font-weight: 700; }
tr.elfogyott .maradt { color: #B3441E; }
.ar-nelkul { color: #6B7A4F; font-family: inherit; font-size: 11px; }
.ures { color: #6B7A4F; text-align: center; padding: 16px; }
@media(max-width:650px){ .modal{max-width:100%;padding:14px}.modal table{min-width:680px} }
</style>
