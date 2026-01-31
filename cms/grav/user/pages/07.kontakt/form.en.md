---
title: Contact
slug: contact
template: form
metadata:
    description: "Contact the Kalkimo Team."
form:
    name: contact
    action: /contact
    fields:
        - name: name
          label: Name
          type: text
          validate:
              required: true
        - name: email
          label: Email
          type: email
          validate:
              required: true
        - name: subject
          label: Subject
          type: select
          options:
              general: General inquiry
              support: Technical support
              feedback: Feedback
              press: Press
        - name: message
          label: Message
          type: textarea
          validate:
              required: true
        - name: privacy
          label: "I have read the privacy policy and agree."
          type: checkbox
          validate:
              required: true
        - name: honeypot
          type: honeypot
    buttons:
        - type: submit
          value: Send message
          classes: btn-primary
    process:
        - email:
              subject: "[Kalkimo] Contact inquiry from {{ form.value.name }}"
              body: "{% include 'forms/data.html.twig' %}"
        - message: "Thank you! Your message has been sent."
        - display: /contact
---

# Contact

Have questions about Kalkimo Planner? Write to us – we'll get back to you as soon as possible.
