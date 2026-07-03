import base64
import json
import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from app.auth import subject_from_token


def _jwt(claims: dict) -> str:
    def seg(d):
        raw = base64.urlsafe_b64encode(json.dumps(d).encode()).decode().rstrip("=")
        return raw
    return f"{seg({'alg': 'none'})}.{seg(claims)}.sig"


class SubjectFromTokenTest(unittest.TestCase):
    def test_reads_sub_claim(self):
        self.assertEqual(subject_from_token(_jwt({"sub": "user-123"})), "user-123")

    def test_falls_back_to_username(self):
        self.assertEqual(subject_from_token(_jwt({"preferred_username": "gaja"})), "gaja")

    def test_opaque_token_is_stable_pseudonym(self):
        a = subject_from_token("opaque-token-abc")
        b = subject_from_token("opaque-token-abc")
        self.assertEqual(a, b)
        self.assertTrue(a.startswith("anon:"))
        self.assertNotEqual(a, subject_from_token("different-token"))


if __name__ == "__main__":
    unittest.main()
