<template>
  <ion-page>
    <ion-content :fullscreen="true" class="login-content">
      <div class="login-layout">
        <!-- Markenfläche -->
        <aside class="brand-pane">
          <KalkLogo variant="dark" />

          <div class="brand-body">
            <h1 class="brand-headline">{{ t('loginPage.headline') }}</h1>
            <p class="brand-intro">{{ t('loginPage.intro') }}</p>

            <ul class="brand-points">
              <li v-for="key in ['point1', 'point2', 'point3']" :key="key">
                <span class="point-rule" aria-hidden="true"></span>
                {{ t(`loginPage.${key}`) }}
              </li>
            </ul>
          </div>

          <p class="brand-legal">
            <ion-icon :icon="lockClosedOutline" />
            {{ t('loginPage.legal') }}
          </p>
        </aside>

        <!-- Formular -->
        <main class="form-pane">
          <div class="form-top">
            <KalkLogo class="mobile-logo" />
            <div class="lang-switch" role="group" aria-label="Sprache / Language">
              <button type="button" :class="{ active: locale === 'de' }" @click="setLocale('de')">DE</button>
              <button type="button" :class="{ active: locale === 'en' }" @click="setLocale('en')">EN</button>
            </div>
          </div>

          <div class="form-wrap">
            <h2 class="form-title">
              {{ isRegisterMode ? t('loginPage.registerTitle') : t('loginPage.welcome') }}
            </h2>
            <p class="form-intro">
              {{ isRegisterMode ? t('loginPage.registerText') : t('loginPage.welcomeText') }}
            </p>

            <!-- Login -->
            <form v-if="!isRegisterMode" novalidate @submit.prevent="handleLogin">
              <KalkInput
                v-model="email"
                :label="t('auth.email')"
                type="email"
                required
                :error-message="errors.email"
              />

              <KalkInput
                v-model="password"
                :label="t('auth.password')"
                type="password"
                required
                :error-message="errors.password"
              />

              <div class="form-row-end">
                <a href="#" class="subtle-link" @click.prevent="forgotPassword">
                  {{ t('auth.forgotPassword') }}
                </a>
              </div>

              <button type="submit" class="btn btn-primary btn-block submit-button" :disabled="isLoading">
                <ion-spinner v-if="isLoading" name="lines-small" class="btn-spinner" />
                <span v-else>{{ t('auth.login') }}</span>
              </button>
            </form>

            <!-- Registrierung -->
            <form v-else novalidate @submit.prevent="handleRegister">
              <KalkInput
                v-model="name"
                :label="t('auth.name')"
                type="text"
                required
                :error-message="errors.name"
              />

              <KalkInput
                v-model="email"
                :label="t('auth.email')"
                type="email"
                required
                :error-message="errors.email"
              />

              <KalkInput
                v-model="password"
                :label="t('auth.password')"
                type="password"
                required
                :error-message="errors.password"
              />

              <KalkInput
                v-model="passwordConfirm"
                :label="t('auth.passwordConfirm')"
                type="password"
                required
                :error-message="errors.passwordConfirm"
              />

              <div v-if="passwordRequirements.length > 0" class="callout password-requirements">
                <div>
                  <p class="requirements-title">{{ t('loginPage.pwRequirements') }}</p>
                  <ul>
                    <li v-for="req in passwordRequirements" :key="req">{{ req }}</li>
                  </ul>
                </div>
              </div>

              <button type="submit" class="btn btn-primary btn-block submit-button" :disabled="isLoading">
                <ion-spinner v-if="isLoading" name="lines-small" class="btn-spinner" />
                <span v-else>{{ t('auth.register') }}</span>
              </button>
            </form>

            <p class="mode-switch">
              <template v-if="!isRegisterMode">
                {{ t('loginPage.noAccount') }}
                <a href="#" @click.prevent="isRegisterMode = true">{{ t('auth.register') }}</a>
              </template>
              <template v-else>
                {{ t('loginPage.haveAccount') }}
                <a href="#" @click.prevent="isRegisterMode = false">{{ t('auth.login') }}</a>
              </template>
            </p>

            <div class="divider"><span>{{ t('loginPage.or') }}</span></div>

            <button type="button" class="btn btn-outline btn-block" @click="continueAsGuest">
              {{ t('loginPage.guest') }}
            </button>
          </div>
        </main>
      </div>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import {
  IonPage,
  IonContent,
  IonSpinner,
  IonIcon,
  menuController,
  onIonViewWillEnter,
  onIonViewWillLeave
} from '@ionic/vue';
import { lockClosedOutline } from 'ionicons/icons';
import { KalkInput } from '@/components';
import KalkLogo from '@/components/layout/KalkLogo.vue';
import { useAuthStore } from '@/stores/authStore';
import { useUiStore } from '@/stores/uiStore';
import { useProjectStore } from '@/stores/projectStore';
import { authApi } from '@/api';
import { setLocale } from '@/i18n';

const { t, locale } = useI18n();
const router = useRouter();
const authStore = useAuthStore();
const uiStore = useUiStore();
const projectStore = useProjectStore();

// Gast-Projekte ins Konto übernehmen; ein Fehler darf den Login nicht blockieren
async function adoptGuestProjects() {
  try {
    const adopted = await projectStore.adoptGuestProjects();
    if (adopted > 0) {
      uiStore.showToast(
        adopted === 1
          ? 'Lokales Projekt wurde in Ihr Konto übernommen'
          : `${adopted} lokale Projekte wurden in Ihr Konto übernommen`,
        'success'
      );
    }
  } catch (e) {
    console.error('Failed to adopt guest projects:', e);
  }
}

const isRegisterMode = ref(false);
const name = ref('');
const email = ref('');
const password = ref('');
const passwordConfirm = ref('');
const isLoading = ref(false);
const errors = ref<{ name?: string; email?: string; password?: string; passwordConfirm?: string }>({});

// Seitenleiste auf der Anmeldeseite ausblenden
onIonViewWillEnter(() => {
  menuController.enable(false);
});

onIonViewWillLeave(() => {
  menuController.enable(true);
});

const passwordRequirements = computed(() => {
  if (!isRegisterMode.value || !password.value) return [];

  const reqs: string[] = [];
  if (password.value.length < 8) reqs.push('Mindestens 8 Zeichen');
  if (!/[A-Z]/.test(password.value)) reqs.push('Mindestens ein Grossbuchstabe');
  if (!/[a-z]/.test(password.value)) reqs.push('Mindestens ein Kleinbuchstabe');
  if (!/\d/.test(password.value)) reqs.push('Mindestens eine Ziffer');
  if (!/[!@#$%^&*(),.?":{}|<>_\-+=\[\]\\\/~`]/.test(password.value)) reqs.push('Mindestens ein Sonderzeichen');
  return reqs;
});

async function handleLogin() {
  errors.value = {};

  if (!email.value) {
    errors.value.email = t('errors.required');
    return;
  }

  if (!password.value) {
    errors.value.password = t('errors.required');
    return;
  }

  isLoading.value = true;

  try {
    const { user, tokens } = await authApi.login({
      email: email.value,
      password: password.value
    });

    authStore.setAuth(user, tokens);
    await adoptGuestProjects();
    router.push('/');
  } catch (error) {
    uiStore.showToast(t('auth.invalidCredentials'), 'error');
  } finally {
    isLoading.value = false;
  }
}

async function handleRegister() {
  errors.value = {};

  if (!name.value) {
    errors.value.name = t('errors.required');
    return;
  }

  if (!email.value) {
    errors.value.email = t('errors.required');
    return;
  }

  if (!password.value) {
    errors.value.password = t('errors.required');
    return;
  }

  if (passwordRequirements.value.length > 0) {
    errors.value.password = 'Passwort erfüllt nicht alle Anforderungen';
    return;
  }

  if (password.value !== passwordConfirm.value) {
    errors.value.passwordConfirm = 'Passwörter stimmen nicht überein';
    return;
  }

  isLoading.value = true;

  try {
    const { user, tokens } = await authApi.register({
      email: email.value,
      password: password.value,
      name: name.value
    });

    authStore.setAuth(user, tokens);
    await adoptGuestProjects();
    uiStore.showToast('Registrierung erfolgreich', 'success');
    router.push('/');
  } catch (error: any) {
    const message = error?.message || 'Registrierung fehlgeschlagen';
    uiStore.showToast(message, 'error');
  } finally {
    isLoading.value = false;
  }
}

function forgotPassword() {
  uiStore.showToast('Funktion noch nicht verfügbar', 'info');
}

function continueAsGuest() {
  router.push('/home');
}
</script>

<style scoped>
.login-content {
  --background: var(--kalk-surface);
}

.login-layout {
  display: grid;
  grid-template-columns: minmax(380px, 5fr) 7fr;
  min-height: 100%;
}

/* Markenfläche */
.brand-pane {
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  gap: var(--kalk-space-10);
  padding: var(--kalk-space-10) var(--kalk-space-12);
  background: var(--kalk-navy-900);
  color: #ffffff;
  border-right: 1px solid var(--kalk-navy-950);
}

.brand-body {
  max-width: 440px;
}

.brand-headline {
  font-family: var(--kalk-font-serif);
  font-size: 2.25rem;
  font-weight: 600;
  line-height: 1.15;
  color: #ffffff;
  letter-spacing: -0.01em;
}

.brand-intro {
  margin-top: var(--kalk-space-4);
  font-size: var(--kalk-text-base);
  line-height: 1.6;
  color: rgba(255, 255, 255, 0.72);
}

.brand-points {
  list-style: none;
  margin: var(--kalk-space-8) 0 0;
  padding: 0;
  border-top: 1px solid rgba(255, 255, 255, 0.12);
}

.brand-points li {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-3);
  padding: var(--kalk-space-3) 0;
  font-size: var(--kalk-text-sm);
  color: rgba(255, 255, 255, 0.88);
  border-bottom: 1px solid rgba(255, 255, 255, 0.12);
}

.point-rule {
  width: 14px;
  height: 2px;
  flex-shrink: 0;
  background: var(--kalk-brass-500);
}

.brand-legal {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-2);
  font-size: var(--kalk-text-xs);
  color: rgba(255, 255, 255, 0.5);
}

/* Formular */
.form-pane {
  display: flex;
  flex-direction: column;
  padding: var(--kalk-space-6) var(--kalk-space-8);
}

.form-top {
  display: flex;
  align-items: center;
  justify-content: flex-end;
}

.mobile-logo {
  display: none;
}

.form-wrap {
  width: 100%;
  max-width: 380px;
  margin: auto;
  padding: var(--kalk-space-10) 0;
}

.form-title {
  font-family: var(--kalk-font-serif);
  font-size: var(--kalk-text-2xl);
  font-weight: 600;
  color: var(--kalk-navy-900);
}

.form-intro {
  margin: var(--kalk-space-2) 0 var(--kalk-space-6);
  font-size: var(--kalk-text-sm);
}

.form-row-end {
  display: flex;
  justify-content: flex-end;
  margin-top: calc(-1 * var(--kalk-space-2));
}

.subtle-link {
  font-size: var(--kalk-text-xs);
  color: var(--kalk-gray-600);
  text-decoration: none;
}

.subtle-link:hover {
  color: var(--kalk-navy-700);
  text-decoration: underline;
}

.submit-button {
  margin-top: var(--kalk-space-5);
  height: 44px;
}

.btn-spinner {
  color: #ffffff;
  --color: #ffffff;
  width: 18px;
  height: 18px;
}

.password-requirements {
  margin-top: var(--kalk-space-2);
}

.requirements-title {
  font-weight: 600;
  color: var(--kalk-gray-800);
  margin-bottom: 4px;
}

.password-requirements ul {
  margin: 0;
  padding-left: var(--kalk-space-4);
  color: var(--kalk-gray-600);
}

.mode-switch {
  margin-top: var(--kalk-space-5);
  font-size: var(--kalk-text-sm);
  text-align: center;
  color: var(--kalk-gray-600);
}

.mode-switch a {
  font-weight: 500;
}

.divider {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-3);
  margin: var(--kalk-space-5) 0;
  font-size: var(--kalk-text-2xs);
  font-weight: 600;
  letter-spacing: var(--kalk-tracking-caps);
  text-transform: uppercase;
  color: var(--kalk-gray-400);
}

.divider::before,
.divider::after {
  content: '';
  flex: 1;
  height: 1px;
  background: var(--kalk-border);
}

.lang-switch {
  display: inline-flex;
  padding: 2px;
  background: var(--kalk-gray-100);
  border-radius: var(--kalk-radius-md);
}

.lang-switch button {
  height: 28px;
  min-width: 32px;
  padding: 0 8px;
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-2xs);
  font-weight: 600;
  letter-spacing: 0.06em;
  color: var(--kalk-gray-500);
  background: transparent;
  border: 0;
  border-radius: 2px;
  cursor: pointer;
}

.lang-switch button.active {
  background: var(--kalk-surface);
  color: var(--kalk-navy-900);
  box-shadow: 0 1px 2px rgba(11, 31, 54, 0.12);
}

@media (max-width: 900px) {
  .login-layout {
    grid-template-columns: 1fr;
  }

  .brand-pane {
    display: none;
  }

  .form-pane {
    padding: var(--kalk-space-4) var(--kalk-space-5);
  }

  .form-top {
    justify-content: space-between;
    padding-top: env(safe-area-inset-top);
  }

  .mobile-logo {
    display: inline-flex;
  }

  .form-wrap {
    padding: var(--kalk-space-8) 0;
  }
}
</style>
