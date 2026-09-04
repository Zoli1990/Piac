<script setup>
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from './stores/auth'
import CalculatorModal from './components/CalculatorModal.vue'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const bejelentkezve = computed(() => !!auth.token && route.name !== 'login')
const showCalculator = ref(false)

function kijelentkezes() {
  auth.logout()
  router.push({ name: 'login' })
}
</script>

<template>
  <header v-if="bejelentkezve">
    <div class="brand">
      <div class="crate-mark"></div>
      <div><div class="brand-text">Rekesz</div><div class="brand-sub">Piaci rekeszkövetés</div></div>
    </div>
    <nav>
      <RouterLink v-if="auth.role === 'Admin'" to="/" :class="{ active: route.name === 'felvasarlas' }">Felvásárlás</RouterLink>
      <RouterLink v-if="auth.role === 'Admin'" to="/eladas" :class="{ active: route.name === 'eladas' }">Eladás</RouterLink>
      <RouterLink to="/egyenleg" :class="{ active: route.name === 'egyenleg' }">Egyenleg</RouterLink>
    </nav>
    <div class="user">
      <button class="calc-button" title="Számológép" @click="showCalculator = true">🧮 <span class="calc-label">Számológép</span></button>
      <span>{{ auth.felhasznalonev }}</span>
      <button class="btn-secondary" @click="kijelentkezes">Kilépés</button>
    </div>
  </header>

  <main :class="{ 'no-header': !bejelentkezve }"><RouterView /></main>
  <CalculatorModal v-if="showCalculator" @close="showCalculator = false" />
</template>

<style scoped>
header { background: var(--chalk-green); color: var(--paper); padding: 12px 20px; display:flex; align-items:center; gap:20px; flex-wrap:wrap; position:sticky; top:0; z-index:10; }
.brand { display:flex; align-items:center; gap:10px; }
.crate-mark { width:26px; height:22px; border:2px solid var(--paper); border-radius:3px; position:relative; }
.crate-mark::before,.crate-mark::after { content:""; position:absolute; top:0; bottom:0; width:2px; background:var(--paper); }
.crate-mark::before { left:33%; } .crate-mark::after { left:66%; }
.brand-text { font-weight:700; font-size:18px; line-height:1; } .brand-sub { font-size:10px; opacity:.65; text-transform:uppercase; letter-spacing:.06em; }
nav { display:flex; gap:4px; flex:1; }
nav a { color:rgba(247,243,232,.65); text-decoration:none; font-weight:600; font-size:13.5px; padding:8px 14px; border-radius:6px; }
nav a.active { background:var(--paper); color:var(--chalk-green); }
.user { display:flex; align-items:center; gap:10px; font-size:13px; }
.calc-button { border:0; background:var(--kapia); color:var(--chalk-green); cursor:pointer; font-size:15px; font-weight:700; padding:8px 12px; border-radius:20px; display:flex; align-items:center; gap:6px; }
.calc-button:hover { filter:brightness(1.08); }
.calc-label { font-size:12px; }
main { max-width:1100px; margin:0 auto; padding:20px; } main.no-header { max-width:420px; padding-top:80px; }
@media (max-width:640px) { header{padding:10px 14px;gap:10px}.brand-sub{display:none}nav{order:3;width:100%;justify-content:space-between}nav a{flex:1;text-align:center;padding:9px 6px}.user span{display:none}.calc-label{display:none}.calc-button{padding:8px 10px}main{padding:14px}main.no-header{padding-top:60px;max-width:100%} }
</style>
