<template>
  <ion-page>
    <ion-content :fullscreen="true" class="login-content">
      <div class="login-container">
        <div class="login-header">
          <h1>{{ t('common.appName') }}</h1>
          <p>Immobilien-Investitionsrechner</p>
        </div>

        <KalkCard class="login-card">
          <!-- Login Form -->
          <form v-if="!isRegisterMode" @submit.prevent="handleLogin">
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

            <ion-button
              type="submit"
              expand="block"
              :disabled="isLoading"
              class="submit-button"
            >
              <ion-spinner v-if="isLoading" name="crescent" />
              <span v-else>{{ t('auth.login') }}</span>
            </ion-button>
          </form>

          <!-- Register Form -->
          <form v-else @submit.prevent="handleRegister">
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

            <div v-if="passwordRequirements.length > 0" class="password-requirements">
              <p class="requirements-title">Passwort-Anforderungen:</p>
              <ul>
                <li v-for="req in passwordRequirements" :key="req">{{ req }}</li>
              </ul>
            </div>

            <ion-button
              type="submit"
              expand="block"
              :disabled="isLoading"
              class="submit-button"
            >
              <ion-spinner v-if="isLoading" name="crescent" />
              <span v-else>{{ t('auth.register') }}</span>
            </ion-button>
          </form>

          <div class="login-footer">
            <template v-if="!isRegisterMode">
              <p class="mode-switch">
                Noch kein Konto?
                <a href="#" @click.prevent="isRegisterMode = true">Registrieren</a>
              </p>
              <a href="#" @click.prevent="forgotPassword" class="forgot-link">
                {{ t('auth.forgotPassword') }}
              </a>
            </template>
            <template v-else>
              <p class="mode-switch">
                Bereits registriert?
                <a href="#" @click.prevent="isRegisterMode = false">Anmelden</a>
              </p>
            </template>
          </div>
        </KalkCard>
      </div>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { IonPage, IonContent, IonButton, IonSpinner } from '@ionic/vue';
import { KalkCard, KalkInput } from '@/components';
import { useAuthStore } from '@/stores/authStore';
import { useUiStore } from '@/stores/uiStore';
import { authApi } from '@/api';

const { t } = useI18n();
const router = useRouter();
const authStore = useAuthStore();
const uiStore = useUiStore();

const isRegisterMode = ref(false);
const name = ref('');
const email = ref('');
const password = ref('');
const passwordConfirm = ref('');
const isLoading = ref(false);
const errors = ref<{ name?: string; email?: string; password?: string; passwordConfirm?: string }>({});

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
    uiStore.showToast('Registrierung erfolgreich!', 'success');
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
</script>

<style scoped>
.login-content {
  --background: var(--kalk-primary-900);
}

.login-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 100%;
  padding: var(--kalk-space-6);
}

.login-header {
  text-align: center;
  color: #ffffff;
  margin-bottom: var(--kalk-space-8);
}

.login-header h1 {
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-3xl);
  font-weight: 700;
  margin: 0;
}

.login-header p {
  opacity: 0.8;
  margin-top: var(--kalk-space-1);
}

.login-card {
  width: 100%;
  max-width: 400px;
}

.submit-button {
  margin-top: var(--kalk-space-6);
  --background: var(--kalk-accent-500);
}

.password-requirements {
  margin-top: var(--kalk-space-2);
  padding: var(--kalk-space-3);
  background: var(--kalk-gray-50);
  border-radius: var(--kalk-radius-md);
  font-size: var(--kalk-text-sm);
}

.password-requirements .requirements-title {
  color: var(--kalk-gray-700);
  font-weight: 500;
  margin: 0 0 var(--kalk-space-2) 0;
}

.password-requirements ul {
  margin: 0;
  padding-left: var(--kalk-space-4);
  color: var(--kalk-gray-600);
}

.password-requirements li {
  margin-bottom: var(--kalk-space-1);
}

.login-footer {
  text-align: center;
  margin-top: var(--kalk-space-6);
}

.mode-switch {
  color: var(--kalk-gray-600);
  font-size: var(--kalk-text-sm);
  margin: 0 0 var(--kalk-space-3) 0;
}

.mode-switch a {
  color: var(--kalk-accent-500);
  font-weight: 500;
  text-decoration: none;
}

.mode-switch a:hover {
  text-decoration: underline;
}

.forgot-link {
  color: var(--kalk-gray-500);
  font-size: var(--kalk-text-sm);
  text-decoration: none;
}

.forgot-link:hover {
  color: var(--kalk-accent-500);
}
</style>
