---
title: Kontakt
slug: kontakt
template: form
metadata:
    description: "Kontaktieren Sie das Kalkimo Team."
form:
    name: kontakt
    action: /kontakt
    fields:
        - name: name
          label: Name
          type: text
          validate:
              required: true
        - name: email
          label: E-Mail
          type: email
          validate:
              required: true
        - name: subject
          label: Betreff
          type: select
          options:
              allgemein: Allgemeine Anfrage
              support: Technischer Support
              feedback: Feedback
              presse: Presse
        - name: message
          label: Nachricht
          type: textarea
          validate:
              required: true
        - name: datenschutz
          label: "Ich habe die Datenschutzerklärung gelesen und bin einverstanden."
          type: checkbox
          validate:
              required: true
        - name: honeypot
          type: honeypot
    buttons:
        - type: submit
          value: Nachricht senden
          classes: btn-primary
    process:
        - email:
              subject: "[Kalkimo] Kontaktanfrage von {{ form.value.name }}"
              body: "{% include 'forms/data.html.twig' %}"
        - message: "Vielen Dank! Ihre Nachricht wurde gesendet."
        - display: /kontakt
---

# Kontakt

Haben Sie Fragen zum Kalkimo Planner? Schreiben Sie uns – wir melden uns so schnell wie möglich.
